using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

using System;
using System.IO;
using System.Linq;
using Random=UnityEngine.Random;
using System.Collections.Generic;

public class AgentInclination : Agent
{
     [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 2f;

    //private Rigidbody playerRigidbody;
    protected Rigidbody _rb;

    Vector3 initialTargetPosition;

    private string [][] inclination;

    public override void Initialize()
    {
        
        _rb = GetComponent<Rigidbody>();    
        initialTargetPosition = target.transform.localPosition;
        inclination = getInclination();
        
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

        //////////////////////Observaciones realcionadas con el terreno////////////////////////////////////////

        sensor.AddObservation(getInclinationBydirection(Vector3.forward));
        sensor.AddObservation(getInclinationBydirection(Vector3.back));
        sensor.AddObservation(getInclinationBydirection(Vector3.left));
        sensor.AddObservation(getInclinationBydirection(Vector3.right));

        sensor.AddObservation(getInclinationBydirection(new Vector3(1,0,1)));
        sensor.AddObservation(getInclinationBydirection(new Vector3(1,0,-1)));
        sensor.AddObservation(getInclinationBydirection(new Vector3(-1,0,1)));
        sensor.AddObservation(getInclinationBydirection(new Vector3(-1,0,-1)));
        
    }

    public string [][] getInclination() {

        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes\inclination.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();

    }

    public float getInclinationAverage(Vector3 actualPosition, Vector3 direction, int difference) {
        float accumulateAverage = 0;
        int i = 0;
        
        while (i < difference) {
            float actualInclination = float.Parse(inclination[(int)transform.localPosition.z][(int)transform.localPosition.x]);
            accumulateAverage += actualInclination;
            i += 1;
        }
        //Debug.Log(accumulateAverage/difference);
        return accumulateAverage/difference;

    }

    public float [] getInclinationBydirection(Vector3 direction) { 

        int[] checkPoints = {15, 30, 60, 120};
        Vector3 actualPosition = transform.localPosition;
        List<float> averages = new List<float>();
        
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
            
            float avegrageInclination = getInclinationAverage(actualPosition, direction, difference);                
        
            averages.Add(avegrageInclination);


            actualPosition = auxPosition;

            i += 1;
        }

        float [] finalAverage =  averages.ToArray();
       
        return finalAverage;
      
    }

    

    //Calcula una posición random para el Target dentro del Terreno
    public Vector3 getTargetStartedPosition(){


        Vector3 posDef = new Vector3(0,0,0);
        bool ok = false;
        while (!ok) {
            float x = Random.Range(-350f,350f);
            float z = Random.Range(-350f,350f);
            Vector3 targetPos = new Vector3(initialTargetPosition.x+x,0,initialTargetPosition.z+z);
            //Debug.Log(agentPos.ToString());
            if (correctPosition(targetPos)) {
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
            float z = Random.Range(-200f,200f);
            Vector3 agentPos = new Vector3(targetPos.x+x,0,targetPos.z+z);
            //Debug.Log(agentPos.ToString());
            if (correctPosition(agentPos)) {
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

        if (pos.x > 9820 || pos.x < 0f) return false;
        if (pos.z > 10020f || pos.z < 0f) return false;
        float actualInclination = float.Parse(inclination[(int)pos.z][(int)pos.x]);
        if (actualInclination > 140f) return false;
        return true;
    }

    public bool getInclinationAndSetReward() {
     
        float actualHeigh = Terrain.activeTerrain.SampleHeight(transform.localPosition) + Terrain.activeTerrain.GetPosition().y;
        Vector3 futurePos = transform.localPosition + transform.forward*2;
        float directionHeight = Terrain.activeTerrain.SampleHeight(futurePos) + Terrain.activeTerrain.GetPosition().y;
            
        float actualInclination = float.Parse(inclination[(int)transform.localPosition.z][(int)transform.localPosition.x]);
        //Debug.Log("Inclinacion: "+actualInclination);
        if (actualInclination > 140f) {
            
            if (directionHeight > actualHeigh) {
                //Debug.Log("Mucha Inclinacion");
                transform.localPosition -= transform.forward*2;
                AddReward(-1f / MaxStep);
                return false;
                
            }
            else {
                Debug.Log("Muero por inclinacion");
                AddReward(-1f);
                EndEpisode();
            }    
        }
        
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

        if (getInclinationAndSetReward()) {

            _rb.MovePosition(transform.position +
            transform.forward * lForward * _speed * Time.fixedDeltaTime);
            transform.Rotate(transform.up * lTurn * _turnSpeed * Time.fixedDeltaTime);

            if (!correctPosition(transform.localPosition)) {
                AddReward(-1f);
                EndEpisode();
            }

            var distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
            
            if (distanceToTarget > 800.0f)
            {
                Debug.Log("Too Far");
                AddReward(-1f); // Penalise for going too far away
                EndEpisode();
            }

        }
          
        AddReward(-1f / MaxStep);
      
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
            SetReward(1.5f);
            Debug.Log("hit");
            EndEpisode();         
        }  
    }
}
  