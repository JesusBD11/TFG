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


public class AgentWithClasses : Agent
{
    [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 15f;

    //private Rigidbody playerRigidbody;
    protected Rigidbody _rb;

    Vector3 initialTargetPosition;

    private bool [,] classes;

    public bool training = true;
 
    public override void Initialize()
    {
        
        _rb = GetComponent<Rigidbody>();    
        initialTargetPosition = target.transform.localPosition;
        if (!training) MaxStep = 0;
        string [][] aux = getClasses();
        classes = preprossecing(aux);
    }


    public override void OnEpisodeBegin()
    {
         
        transform.LookAt(target.transform);

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        target.transform.localPosition = getTargetStartedPosition();
        
        transform.localPosition = getAgentPosition();
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        
         // Distancia al Target
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, target.transform.localPosition));

        //Hacia donde mira
        sensor.AddObservation(transform.forward);

        //Direción del objetivo
        sensor.AddObservation((target.transform.localPosition - transform.localPosition).normalized);

        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
   
        addObservations(sensor, getclassesBydirection(transform.forward));
        addObservations(sensor, getclassesBydirection(Vector3.back));
        addObservations(sensor, getclassesBydirection(Vector3.left));
        addObservations(sensor, getclassesBydirection(Vector3.right));
        /*
        addObservations(sensor, getclassesBydirection(new Vector3(1,0,1)));
        addObservations(sensor, getclassesBydirection(new Vector3(1,0,-1)));
        addObservations(sensor, getclassesBydirection(new Vector3(-1,0,1)));
        addObservations(sensor, getclassesBydirection(new Vector3(-1,0,-1)));*/
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

        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes\classes.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }

    public bool isWater(Vector3 pos) {


        if(!correctPosition(pos)) return true;

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
        Vector3 actualPosition = transform.localPosition;
        List<bool> results = new List<bool>();
        
        int difference;
        Vector3 auxPosition;
        int i = 0;        

        while (i <= 3) {

            if (i > 0) {
                difference = checkPoints[i] - checkPoints[i-1];
                auxPosition = actualPosition + direction*difference;
                
            }
            else {
                difference = checkPoints[i];
                auxPosition = actualPosition + direction*difference;
            }
            
            bool classesByInterval = getClass(actualPosition, direction, difference);                
            results.Add(classesByInterval);


            actualPosition = auxPosition;

            i += 1;
        }

        bool [] finalResul =  results.ToArray();
       
        return finalResul;
      
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
        Vector3 targetPos = target.transform.localPosition;
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

        if (pos.x >= 9820 || pos.x <= 0f) return false;
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

        

        _rb.MovePosition(transform.position +
        transform.forward * lForward * _speed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * lTurn * _turnSpeed * Time.fixedDeltaTime);
    
    
        if (!correctPosition(transform.localPosition)) {
            if (training) AddReward(-1f);
            EndEpisode();
        }

        //bool tipoTerreno = classes[(int)transform.localPosition.z,(int)transform.localPosition.x]; 
        //Debug.Log("tipo:" + tipoTerreno);

        if (isWater(transform.localPosition)) {
            if (training) AddReward(-1f);
            EndEpisode();
        }

        var distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        
        if (distanceToTarget > 600.0f)
        {
            Debug.Log("Too Far");
            if (training) AddReward(-1f); // Penalise for going too far away
            EndEpisode();
        }   
        if (training) AddReward(-1f / MaxStep);
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
        if (collision.gameObject.CompareTag("Target") == true)
        {  
            if (training) AddReward(1.5f);
            Debug.Log("hit");
            EndEpisode();         
        }  
    }
}
  