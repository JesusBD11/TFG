using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

using System;
using System.IO;
using System.Linq;
using Random=UnityEngine.Random;
using System.Collections.Generic;
using System.Globalization;

/*public static class Extns
{
    public static Vector2 xz(this Vector3 vv)
    {
        return new Vector2(vv.x, vv.z);
    }    
     
    public static float FlatDistanceTo(this Vector3 from, Vector3 unto)
    {
        Vector2 a = from.xz();
        Vector2 b = unto.xz();
        return Vector2.Distance( a, b );
    }
}*/


public class People : Agent
{
    [SerializeField]
    private GameObject target = null;

    [SerializeField]
    protected GameObject [] people;

    public float _turnSpeed = 50f;
    public float _speed = 15f;

    Vector3 initialTargetPosition;

    private int [,] classes;

    private string [][] clasAux;

    public bool training = true;

    private float distanceCovered = 0;
    private float walkSteps = 0;
    private bool twistedAnkle = false;

    private float MAX_WALK_STEPS = 15000.0f;
    private float SEARCH_RADIUS = 1000.0f;


    private static int GRID_SIZE_Z = 10020;
    private static int GRID_SIZE_X = 9820;
    private static int CLASS_WATER = 1;
    private static int CLASS_DENSE = 2;
    private static int CLASS_ROCKS = 3;
    private static int NUM_CLASSES = 4;

    private static int PEOPLE = 1;

    Vector3 [] initialTargetPositions = new Vector3 [PEOPLE];  

 
    public override void Initialize()
    {
        initialTargetPosition = target.transform.position;
        if (!training) MaxStep = 0;

        preprossecing(getClasses());

        target.transform.position = getTargetStartedPosition();
        saveInitialPos(); //Guarda pos de las demas personas 
        getPeoplepos(); //Coloca random a las personas
    }


    public override void OnEpisodeBegin()
    {         
        transform.position = getAgentPosition();
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        distanceCovered = 0;
        walkSteps = 0;
        twistedAnkle = false;
    }

    public void saveInitialPos () {

        for (int i = 0; i < initialTargetPositions.Length; ++i) {
            
            initialTargetPositions[i] = people[i].transform.position;
            people[i].transform.position = getAgentPosition(); 
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        Vector2 toTarget = (target.transform.position - this.transform.position).xz();
        float dist = toTarget.magnitude;
        toTarget = toTarget.normalized;

        // Distancia al Target
        sensor.AddObservation(dist/SEARCH_RADIUS);

        // Direccion del target
        sensor.AddObservation(toTarget);

        // Direccion forward
        sensor.AddObservation(transform.forward.xz().normalized);

        ////////////Obs relacionadas con el resto de gente/////////////////
        for (int i = 0; i < people.Length; ++i) {
            Vector2 toPeople = (people[i].transform.position - this.transform.position).xz();
            float distP = toPeople.magnitude;
            toPeople = toPeople.normalized;

            // Distancia a la persona
            sensor.AddObservation(distP/SEARCH_RADIUS);

            // Direccion de la persona
            sensor.AddObservation(toPeople);

        }


        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        addObservations(sensor, getClassesByDirection( transform.forward));
        addObservations(sensor, getClassesByDirection((transform.forward + transform.right).normalized));
        addObservations(sensor, getClassesByDirection((transform.forward - transform.right).normalized));
        addObservations(sensor, getClassesByDirection( transform.right));
        addObservations(sensor, getClassesByDirection(-transform.right));
        addObservations(sensor, getClassesByDirection((-transform.forward + transform.right).normalized));
        addObservations(sensor, getClassesByDirection((-transform.forward - transform.right).normalized));
        addObservations(sensor, getClassesByDirection(-transform.forward));
    }

    public void addObservations(VectorSensor sensor, float [] results) {
        for (int i = 0; i < results.Length; ++i) {           
            sensor.AddObservation(results[i]);
        }
    }

    public void preprossecing(string [][] aux) {

        classes = new int[GRID_SIZE_Z,GRID_SIZE_X];

        for(int i = 0; i < GRID_SIZE_Z; ++i) {
            for(int j = 0; j < GRID_SIZE_X; ++j) {
                int tipo = int.Parse(aux[i+1][j+1]);
                if(tipo == 20 || tipo == 37 || tipo == 36 || tipo == 38|| tipo == 39 || tipo == 40 || tipo == 41){
                    classes[i,j] = CLASS_WATER;  
                }
                else if (tipo == 7 || tipo == 8 || tipo == 9 || tipo == 10) {
                    classes[i,j] = CLASS_DENSE;
                }
                else if (tipo == 18) {
                    classes[i,j] = CLASS_ROCKS;
                }
                else {
                    classes[i,j] = 0;
                }
            }
        }
    }

    public string [][] getClasses() {
        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes/classes.csv";
        clasAux = File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }

    public int getClass(Vector3 pos) {
        return classes[(int)pos.z,(int)pos.x];
    }

    public bool isWater(Vector3 pos) {
        return getClass(pos) == CLASS_WATER;
    }

    public bool isRocky(Vector3 pos) {
        return getClass(pos) == CLASS_ROCKS;
    }

    public float[] getClassesByDirection(Vector3 direction) { 

        float[] checkPoints = {15f, 60f, 150f, 300f};
        float[] stepSize  = {1.0f, 2.0f, 4.0f, 8.0f};
        Vector3 actualPosition = transform.position;

        float[] classCount = new float[NUM_CLASSES*checkPoints.Length];

        for (int i = 0; i < checkPoints.Length; i++) {

            for (int j = 0; j < NUM_CLASSES; j++) {
                classCount[i*NUM_CLASSES + j] = 0;
            }
            int totalCount = 0;

            float t = i > 0 ? checkPoints[i-1] : 0;
            while (t < checkPoints[i]) {

                // posicion. Mejor calcular siempre para evitar ir acumulando errores precision
                Vector3 pos = transform.position + t*direction;

                // obtenemos clase
                int c = getClass(pos);

                // contamos
                classCount[i*NUM_CLASSES + c] += 1;
                totalCount += 1;

                // avanzamos
                t += stepSize[i];
            }

            // normalizar: histograma -> frecuencias
            for (int j = 0; j < NUM_CLASSES; j++) {
                classCount[i*NUM_CLASSES + j] /= totalCount;
            }
        }

        return classCount;      
    }
    
    public void getPeoplepos() {

        for (int i = 0; i < people.Length; ++i) {
            
            bool ok = false;
            Vector3 posDef = new Vector3(0,0,0);
            Vector3 targetPos = target.transform.position;
            while (!ok) {
                float x = Random.Range(-300f,300f);
                float z = Random.Range(-300f,300f);
                Vector3 agentPos = new Vector3(targetPos.x+x,0,targetPos.z+z);

                if (correctPosition(agentPos) && !isWater(targetPos)) {
                    ok = true;
                    // set the Y coordinate according to terrain Y at that point:
                    agentPos.y = Terrain.activeTerrain.SampleHeight(agentPos) + Terrain.activeTerrain.GetPosition().y; 
                    // you probably want to create the object a little above the terrain:
                    agentPos.y += 1f; // move position 0.5 above the terrain 

                    posDef = agentPos;
                
                }
                
            }
            people[i].transform.position = posDef;
        }
         
    }

    //Calcula una posición random para el Target dentro del Terreno
    public Vector3 getTargetStartedPosition(){

        Vector3 posDef = new Vector3(0,0,0);
        bool ok = false;
        while (!ok) {
            float x = Random.Range(-1500f,1500f);
            float z = Random.Range(-2000f,2000f);
            Vector3 targetPos = new Vector3(initialTargetPosition.x+x,0,initialTargetPosition.z+z);
            //Debug.Log(agentPos.ToString());
            if (correctPosition(targetPos) && !isWater(targetPos)) {
                ok = true;
                // set the Y coordinate according to terrain Y at that point:
                targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.GetPosition().y; 
                // you probably want to create the object a little above the terrain:
                targetPos.y += 10f; // move position 0.5 above the terrain 

                posDef = targetPos;
            }
    
        }
        return posDef;

    }

    public Vector3 getAgentPosition() {

        bool ok = false;
        Vector3 posDef = new Vector3(0,0,0);
        Vector3 targetPos = target.transform.position;
        while (!ok) {

            float d = Random.Range(30.0f, 100.0f);
            float a = Random.Range(0.0f, 2f * Mathf.PI);
            Vector3 agentPos = new Vector3(
                    targetPos.x + d*Mathf.Cos(a),
                    0,
                    targetPos.z + d*Mathf.Sin(a)
                );

            //float x = Random.Range(-200f,200f);
            //float z = Random.Range(-300f,300f);
            //Vector3 agentPos = new Vector3(targetPos.x+x,0,targetPos.z+z);


            //Debug.Log(agentPos.ToString());
            if (correctPosition(agentPos) && !isWater(targetPos)) {
                ok = true;
                // set the Y coordinate according to terrain Y at that point:
                agentPos.y = Terrain.activeTerrain.SampleHeight(agentPos) + Terrain.activeTerrain.GetPosition().y; 
                // you probably want to create the object a little above the terrain:
                agentPos.y += 1f; // move position 0.5 above the terrain 

                posDef = agentPos;

            }
        }

        return posDef;

    }

    //Comprueba si una posicion dada esta dentro de los limites del terreno
    public bool correctPosition(Vector3 pos) {

        if (pos.x >= GRID_SIZE_X || pos.x <= 0f) return false;
        if (pos.z >= GRID_SIZE_Z || pos.z <= 0f) return false;

        return true;
    }


    public float getdistPeople(Vector3 pos) {

        float minDist = 15000f;
        for (int i = 0; i < people.Length; ++i) {
            float d = pos.FlatDistanceTo(people[i].transform.position);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
                
        float lForward = actions.DiscreteActions[0];
        float lTurn = 0;
        if (actions.DiscreteActions[1] == 1)
        {
          lTurn = -1;
        }
        else if (actions.DiscreteActions[1] == 2)
        {
          lTurn = 1;
        }

        float speedModifier = (twistedAnkle ? 0.5f : 1.0f);
        int terrType = getClass(transform.position);
        if (terrType == CLASS_ROCKS) speedModifier *= 0.3f;  //0.4
        if (terrType == CLASS_DENSE) speedModifier *= 0.7f;  //0.8


        float dist = lForward * _speed * Time.fixedDeltaTime;
        distanceCovered += dist;
        walkSteps += 1; // independientemente de si avanzamos o no, nos cansamos


        transform.position += transform.forward * dist;
        transform.Rotate(transform.up * lTurn * _turnSpeed * Time.fixedDeltaTime);

        Vector3 p = transform.position;
        p.y = 1.0f + Terrain.activeTerrain.SampleHeight(p) + Terrain.activeTerrain.GetPosition().y;
        transform.position = p;

        if (training) {

            AddReward(-1.0f/MAX_WALK_STEPS);

            Debug.Log(clasAux[(int)transform.position.z][(int)transform.position.x]);

            if (!correctPosition(transform.position)) {
                Debug.Log("Out of map");
                AddReward(-1f);
                EndEpisode();
            }

            var distanceToPeople = getdistPeople(transform.position);
            //Debug.Log(distanceToPeople);
            if (distanceToPeople < 100f) {
                AddReward(-1.0f/distanceToPeople);
            }
           

            var distanceToTarget = transform.position.FlatDistanceTo(target.transform.position);
            
            if (distanceToTarget > SEARCH_RADIUS)
            {
                Debug.Log("Too Far");
                AddReward(-1.0f); // Penalise for going too far away
                EndEpisode();
            }

            if (walkSteps > MAX_WALK_STEPS) {
                Debug.Log("Too tired");
                AddReward(-distanceToTarget/SEARCH_RADIUS);
                EndEpisode();
            }

            if (isWater(transform.position)) {
                Debug.Log("Water");
                AddReward(-1f);
                EndEpisode();
            }
            if (isRocky(transform.position)) {
                // probabilidad caida de piedras
                float r = Random.Range(0f, 1f);
                if (r < 0.00001f) {
                    Debug.Log("Rockfall");
                    AddReward(-1f);
                    EndEpisode();                    
                }
                // probabilidad torcerse un pie, avanzara mas lento
                else if (r < 0.0005f) {
                    Debug.Log("Twisted ankle");
                    AddReward(-0.2f);
                    // no acabamos episodio
                    twistedAnkle = true;
                }
            }

            if (distanceToTarget < 30.0f) {  
                AddReward(1.0f);
                Debug.Log("hit at distance " + distanceCovered);
                EndEpisode();      

                target.transform.position = getTargetStartedPosition();
                getPeoplepos();

            }  
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        int lForward = 0;
        int lTurn = 0;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            lForward = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            lTurn = 1;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            lTurn = 2;
        }

        discreteActionsOut[0] = lForward;
        discreteActionsOut[1] = lTurn;
    }

    void OnCollisionEnter(Collision collision)
    {
        /*
        if (collision.gameObject.CompareTag("Target") == true)
        {  
            if (training) AddReward(1.0f);
            Debug.Log("hit at distance " + distanceCovered);
            EndEpisode();      

            target.transform.position = getTargetStartedPosition();   
        }  
        */
    }
}
  