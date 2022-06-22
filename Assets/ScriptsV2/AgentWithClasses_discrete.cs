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


public class AgentWithClasses_discrete : Agent
{
    [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 15f;

    Vector3 initialTargetPosition;

    private bool [,] classes;

    public bool training = true;

    private float distanceCovered = 0;

    private float MAX_WALK_DIST = 5000.0f;
    private float SEARCH_RADIUS = 500.0f;
 
    public override void Initialize()
    {
        initialTargetPosition = target.transform.position;
        if (!training) MaxStep = 0;
        //string [][] aux = getClasses();
        //classes = preprossecing(aux);
    }


    public override void OnEpisodeBegin()
    {         
        target.transform.position = getTargetStartedPosition();
        
        transform.position = getAgentPosition();
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        distanceCovered = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        Vector3 toTarget = target.transform.position - this.transform.position;
        toTarget[1] = 0;
        float dist = toTarget.magnitude;
        toTarget = toTarget.normalized;

         // Distancia al Target
        sensor.AddObservation(dist/SEARCH_RADIUS);

        //Direción del objetivo
        sensor.AddObservation(toTarget[0]);
        sensor.AddObservation(toTarget[2]);



        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
   
        /*addObservations(sensor, getclassesBydirection( transform.forward));
        addObservations(sensor, getclassesBydirection(-transform.forward));
        addObservations(sensor, getclassesBydirection( transform.right));
        addObservations(sensor, getclassesBydirection(-transform.right));
        addObservations(sensor, getclassesBydirection((transform.forward + transform.right).normalized));
        addObservations(sensor, getclassesBydirection((-transform.forward + transform.right).normalized));
        addObservations(sensor, getclassesBydirection((transform.forward - transform.right).normalized));
        addObservations(sensor, getclassesBydirection((-transform.forward - transform.right).normalized));*/
    }

    public void addObservations(VectorSensor sensor, bool [] results) {
        for (int i = 0; i < 4; ++i) {           
            sensor.AddObservation(results[i]);
        }
    }

    public bool [,] preprossecing(string [][] aux) {


        bool [,] preprossecingClasses = new bool [10020,9820];
        for(int i = 0; i < 10020; ++i) {
            for(int j = 0; j < 9820; ++j) {
                float tipo = float.Parse(aux[i+1][j+1]);
                if(tipo == 20f || tipo == 37f || tipo == 36f){
                    preprossecingClasses[i,j] = true;
                }
                else{
                    preprossecingClasses[i,j] = false;
                }
            }
        }
        return preprossecingClasses;

    }

    public string [][] getClasses() {

        var filePath = @"/home/data/Unity/tfg_jesus/classes.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }

    public bool isWater(Vector3 pos) {

        return false;
/*
        if(!correctPosition(pos)) return true;

        bool tipoTerreno = classes[(int)pos.z,(int)pos.x];
        return tipoTerreno;
*/
    }

    public bool getClass(Vector3 actualPosition, Vector3 direction, int difference) {

        int i = 0;
        
        while (i < difference) {
            if (isWater(actualPosition)) {
                return true;
            }
            actualPosition += direction;
            i += 1;
        }
        return false;
    } 

    public bool [] getclassesBydirection(Vector3 direction) { 

        int[] checkPoints = {15, 50, 100, 200};
        Vector3 actualPosition = transform.position;

        bool[] results = {false, false, false, false};
        
        int difference;
        Vector3 auxPosition;
        
        for (int i = 0; i < 4; i++) {

            if (i > 0) {
                difference = checkPoints[i] - checkPoints[i-1];
                auxPosition = actualPosition + direction*difference;
                
            }
            else {
                difference = checkPoints[i];
                auxPosition = actualPosition + direction*difference;
            }
                       
            results[i] = getClass(actualPosition, direction, difference);

            actualPosition = auxPosition;
        }
       
        return results;
      
    }


    //Calcula una posición random para el Target dentro del Terreno
    public Vector3 getTargetStartedPosition(){


        Vector3 posDef = new Vector3(0,0,0);
        bool ok = false;
        while (!ok) {
            float x = Random.Range(-200f,200f);
            float z = Random.Range(-300f,300f);
            Vector3 targetPos = new Vector3(initialTargetPosition.x+x,0,initialTargetPosition.z+z);
            //Debug.Log(agentPos.ToString());
            if (correctPosition(targetPos) && !isWater(targetPos)) {
                ok = true;
                // set the Y coordinate according to terrain Y at that point:
                targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.GetPosition().y; 
                // you probably want to create the object a little above the terrain:
                targetPos.y += 20f; // move position 0.5 above the terrain 

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
            float x = Random.Range(-200f,200f);
            float z = Random.Range(-300f,300f);
            Vector3 agentPos = new Vector3(targetPos.x+x,0,targetPos.z+z);
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

        if (pos.x >= 9820f || pos.x <= 0f) return false;
        if (pos.z >= 10020f || pos.z <= 0f) return false;

        return true;
    }



    public override void OnActionReceived(ActionBuffers actions)
    {
        
        float direction = actions.DiscreteActions[0];

        Vector3 dir = Vector3.zero;
        if (direction == 0) dir =  new Vector3( 1, 0, 0);
        if (direction == 1) dir = (new Vector3( 1, 0, 1)).normalized;
        if (direction == 2) dir =  new Vector3( 0, 0, 1);
        if (direction == 3) dir = (new Vector3(-1, 0, 1)).normalized;
        if (direction == 4) dir =  new Vector3(-1, 0, 0);
        if (direction == 5) dir = (new Vector3(-1, 0,-1)).normalized;
        if (direction == 6) dir =  new Vector3( 0, 0,-1);
        if (direction == 7) dir = (new Vector3( 1, 0,-1)).normalized;

        float dist = _speed * Time.fixedDeltaTime;
        distanceCovered += dist;

        transform.position += dist*dir;


        if (training) {

            AddReward(-dist/MAX_WALK_DIST);

            if (!correctPosition(transform.position)) {
                Debug.Log("Out of map");
                AddReward(-1f);
                EndEpisode();
            }

            Vector3 toTarget = transform.position - target.transform.position;
            toTarget[1] = 0;
            var distanceToTarget = toTarget.magnitude;

            if (distanceToTarget > SEARCH_RADIUS)
            {
                Debug.Log("Too Far");
                AddReward(-1.0f); // Penalise for going too far away
                EndEpisode();
            }

            if (distanceCovered > MAX_WALK_DIST) {
                Debug.Log("Too tired");
                AddReward(-distanceToTarget/SEARCH_RADIUS);
                EndEpisode();
            }

            /*if (isWater(transform.position)) {
                Debug.Log("Water");
                AddReward(-1f);
                EndEpisode();
            }*/
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        /*
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
        discreteActionsOut[1] = lTurn;*/
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target") == true)
        {  
            if (training) AddReward(1.0f);// - 0.5f*distanceCovered/MAX_WALK_DIST);
            Debug.Log("hit at distance " + distanceCovered);
            EndEpisode();         
        }  
    }
}
  