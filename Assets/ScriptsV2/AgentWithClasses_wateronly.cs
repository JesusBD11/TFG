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


public class AgentWithClasses_wateronly : Agent
{
    [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 15f;

    Vector3 initialTargetPosition;

    private bool [,] classes;

    public bool training = true;

    private float distanceCovered = 0;
    private float walkSteps = 0;

    private float MAX_WALK_STEPS = 10000.0f;
    private float SEARCH_RADIUS = 1000.0f;
 
    public override void Initialize()
    {
        initialTargetPosition = target.transform.position;
        if (!training) MaxStep = 0;

        string [][] aux = getClasses();
        classes = preprossecing(aux);

        target.transform.position = getTargetStartedPosition();
    }


    public override void OnEpisodeBegin()
    {         
        transform.position = getAgentPosition();
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        distanceCovered = 0;
        walkSteps = 0;
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


        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        addObservations(sensor, getclassesBydirection( transform.forward));
        addObservations(sensor, getclassesBydirection((transform.forward + transform.right).normalized));
        addObservations(sensor, getclassesBydirection((transform.forward - transform.right).normalized));
        addObservations(sensor, getclassesBydirection( transform.right));
        addObservations(sensor, getclassesBydirection(-transform.right));
        addObservations(sensor, getclassesBydirection((-transform.forward + transform.right).normalized));
        addObservations(sensor, getclassesBydirection((-transform.forward - transform.right).normalized));
        addObservations(sensor, getclassesBydirection(-transform.forward));
    }

    public void addObservations(VectorSensor sensor, bool [] results) {
        for (int i = 0; i < 4; ++i) {           
            sensor.AddObservation(results[i] ? 1.0f : 0.0f);
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

        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes\classes.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }

    public bool isWater(Vector3 pos) {

        if(!correctPosition(pos)) return false;

        bool tipoTerreno = classes[(int)pos.z,(int)pos.x];
        return tipoTerreno;
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
            float x = Random.Range(-1500f,1500f);
            float z = Random.Range(-2500f,2500f);
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

            float d = Random.Range(300.0f, 600.0f);
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

        if (pos.x >= 9820f || pos.x <= 0f) return false;
        if (pos.z >= 10020f || pos.z <= 0f) return false;

        return true;
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

            if (!correctPosition(transform.position)) {
                Debug.Log("Out of map");
                AddReward(-1f);
                EndEpisode();
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

            if (distanceToTarget < 20.0f) {  
                AddReward(1.0f);
                Debug.Log("hit at distance " + distanceCovered);
                EndEpisode();      

                target.transform.position = getTargetStartedPosition();   
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
  