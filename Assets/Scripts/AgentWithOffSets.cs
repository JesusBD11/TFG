using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

using System;
using System.IO;
using System.Linq;
using Random=UnityEngine.Random;
using System.Collections.Generic;
public class AgentWithOffSets : Agent
{
     [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 2f;

    //private Rigidbody playerRigidbody;
    protected Rigidbody _rb;

    Vector3 initialTargetPosition;


    public override void Initialize()
    {
        
        _rb = GetComponent<Rigidbody>();    
        initialTargetPosition = target.transform.localPosition;
        
    }


    public override void OnEpisodeBegin()
    {
        
        transform.LookAt(target.transform);

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        target.transform.localPosition = initialTargetPosition;//getTargetStartedPosition();
        
        transform.localPosition = getAgentPosition();
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        
        // Distancia al Target
        sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition));

        //Hacia donde mira
        sensor.AddObservation(transform.forward);

        //Direción del objetivo
        sensor.AddObservation((target.transform.localPosition - transform.localPosition).normalized);

        //////////////////////Observaciones realcionadas con el terreno////////////////////////////////////////

        /*sensor.AddObservation(getMaxsAndMinsBydirection(Vector3.forward));
        sensor.AddObservation(getMaxsAndMinsBydirection(Vector3.back));
        sensor.AddObservation(getMaxsAndMinsBydirection(Vector3.left));
        sensor.AddObservation(getMaxsAndMinsBydirection(Vector3.right));

        sensor.AddObservation(getMaxsAndMinsBydirection(new Vector3(1,0,1)));
        sensor.AddObservation(getMaxsAndMinsBydirection(new Vector3(1,0,-1)));
        sensor.AddObservation(getMaxsAndMinsBydirection(new Vector3(-1,0,1)));
        sensor.AddObservation(getMaxsAndMinsBydirection(new Vector3(-1,0,-1)));*/
        
    }

    public (float max,float min) getMaxAndMinHeigh (Vector3 actualPosition, Vector3 direction, int difference) {
        float max = Terrain.activeTerrain.SampleHeight(actualPosition) + Terrain.activeTerrain.GetPosition().y;
        float min = max;
        int i = 0;
        while(i < difference) {
            float height = Terrain.activeTerrain.SampleHeight(actualPosition) + Terrain.activeTerrain.GetPosition().y;
            if (height > max) max = height;
            if (height < min) min = height;
            actualPosition += direction*i;

            i += 1;
        }

        return (max, min);
    }

    public float [] getMaxsAndMinsBydirection(Vector3 direction) { 

        int[] checkPoints = {50, 100, 200, 400};
        Vector3 actualPosition = transform.localPosition;
        List<float> MaxAndMins = new List<float>();
        
        int difference;
        Vector3 auxPosition;
        int i = 0;        
        float actualHeigh = Terrain.activeTerrain.SampleHeight(actualPosition) + Terrain.activeTerrain.GetPosition().y;

        while (i <= 3) {

            if (i > 0) {
                difference = checkPoints[i] - checkPoints[i-1];
                auxPosition = actualPosition + direction*difference;
                
            }
            else {
                difference = checkPoints[i];
                auxPosition = actualPosition + direction*difference;
            }
            
            (float max, float min) = getMaxAndMinHeigh(actualPosition, direction, difference);

            
            if (actualHeigh != min) min = -Math.Abs(actualHeigh-min);
            else min = 0;

            if (actualHeigh != max) max = Math.Abs(actualHeigh-max);
            else max = 0;
                
        
            MaxAndMins.Add(max);
            MaxAndMins.Add(min);

            actualPosition = auxPosition;

            i += 1;
        }

        float [] finalAverage =  MaxAndMins.ToArray();
       
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
            AddReward(1.5f);
            Debug.Log("hit");
            EndEpisode();         
        }  
    }
}
  