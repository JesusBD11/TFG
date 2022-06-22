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


public class CheckPoints : Agent
{
    [SerializeField]
    //private GameObject target = null;
    protected GameObject[] targets;

    public float _turnSpeed = 50f;
    public float _speed = 15f;

    //private Rigidbody playerRigidbody;
    protected Rigidbody _rb;

    Vector3 [] initialTargetPositions = new Vector3 [4];

    public Vector3 initialAgentPos;

    float [] visitedTarget = new float [4];
    public bool training = true;

    float numVistos;

    private Vector3 startedPos;

    public override void Initialize()
    {
        
        _rb = GetComponent<Rigidbody>();
        saveInitialPos();
        if (!training) MaxStep = 0;

    }

    public void saveInitialPos () {

        for (int i = 0; i < initialTargetPositions.Length; ++i) {
            
            initialTargetPositions[i] = targets[i].transform.localPosition; 
        }

    }

    public override void OnEpisodeBegin()
    {
         
        transform.LookAt(this.transform);

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        visitedTarget = new float [4];
        startedPos = transform.localPosition;
        getTargetStartedPosition();
        transform.localPosition = getAgentPosition();
        initialAgentPos = transform.localPosition;
        transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
  
        numVistos = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        
        for (int i = 0; i < targets.Length; ++i) {
            
            // Distancia al Target
            sensor.AddObservation(Vector3.Distance(this.transform.localPosition, targets[i].transform.localPosition));    

            //Direción del objetivo
            sensor.AddObservation((targets[i].transform.localPosition - transform.localPosition).normalized);

        }
        //Hacia donde mira
        sensor.AddObservation(transform.forward);
        //Vector de visitados
        //sensor.AddObservation(visitedTarget);
        for(int i = 0; i<visitedTarget.Length; ++i) {

            if (visitedTarget[i] == 1) sensor.AddObservation(1f);
            else sensor.AddObservation(0f);
        }
        sensor.AddObservation(numVistos/4f);
    }

    //Calcula una posición random para el Target dentro del Terreno
    public void getTargetStartedPosition(){

        for(int i = 0; i < targets.Length; ++i) {
                
            Vector3 posDef = new Vector3(0,0,0);
            bool ok = false;
            while (!ok) {
                float x = Random.Range(-200f,200f);
                float z = Random.Range(-200f,200f);
                Vector3 targetPos = new Vector3(initialTargetPositions[i].x,0,initialTargetPositions[i].z);
                //Debug.Log(agentPos.ToString());
                Collider[] colliders = Physics.OverlapSphere(targetPos, 15f);

                if (correctPosition(targetPos) && colliders.Length == 0) {
                    ok = true;
                    // set the Y coordinate according to terrain Y at that point:
                    targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.GetPosition().y; 
                    // you probably want to create the object a little above the terrain:
                    targetPos.y += 20f; // move position 0.5 above the terrain 

                    posDef = targetPos;
                }
        
            }
            targets[i].transform.localPosition = posDef;

        }

    }

    public Vector3 getAgentPosition() {

        bool ok = false;
        Vector3 posDef = new Vector3(0,0,0);
        Vector3 targetPos = startedPos;
        while (!ok) {
            float x = Random.Range(-150f,150f);
            float z = Random.Range(-150f,150f);
            Vector3 agentPos = new Vector3(targetPos.x+x,0,targetPos.z+z);

            Collider[] colliders = Physics.OverlapSphere(agentPos, 15f);
   
            if (correctPosition(agentPos) && colliders.Length == 0) {
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

        var distanceToTarget = Vector3.Distance(transform.localPosition, initialAgentPos);
        Debug.Log(distanceToTarget);
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

    public bool allvisited() {
        for (int i = 0; i < visitedTarget.Length; ++i) {
            if (visitedTarget[i] == 0) return false;
        }

        return true;
    }

    void printResume() {
        Debug.Log("Morado = " + visitedTarget[0] + "Azul = " + visitedTarget[1] + "Verde = " + visitedTarget[2] + "Amarillo = +" + visitedTarget[3]);
    }

    void OnCollisionEnter(Collision collision)
    {
        for (int i = 1; i <= targets.Length; ++i) {

            if (collision.gameObject.CompareTag("Target"+ i) == true) {  
                if (training) {
                    if (visitedTarget[i-1] == 0f) {
                        numVistos+=1;

                        AddReward(numVistos/4f);
                        if (numVistos == 4) {
                            SetReward(1f);
                            Debug.Log("Todos Vistos");
                            EndEpisode();
                        }

                        /*if (allvisited()) {
                            Debug.Log("Todos Vistos");
                            AddReward(1.5f);
                            EndEpisode();
                        }

                        if (is_first)  {
                            AddReward(0.5f);
                            is_first = false;
                            //Debug.Log("Primero que veo");
                        }
                        else {
                            AddReward(1f);
                            //Debug.Log("Segundo que veo");
                        }*/
                        visitedTarget[i-1] = 1f;
                    }
                    else {
                        AddReward(-0.01f);
                    }
                } 
                printResume();
            }    
           
        }
    }
}
  