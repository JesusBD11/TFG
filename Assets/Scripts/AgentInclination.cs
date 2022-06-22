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

public static class Extns
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
}

public class AgentInclination : Agent
{
     [SerializeField]
    private GameObject target = null;

    public float _turnSpeed = 50f;
    public float _speed = 2f;

    Vector3 initialTargetPosition;

    private float [,] slope;

    private float [,] exposure;

    public bool training = true;

    private float distanceCovered = 0;
    private float walkSteps = 0;

    private static int GRID_SIZE_Z = 10020;
    private static int GRID_SIZE_X = 9820;
    
    private float MAX_WALK_STEPS = 15000.0f;
    private float SEARCH_RADIUS = 1000.0f;

    public override void Initialize()
    {
        
          
        initialTargetPosition = target.transform.localPosition;

        if (!training) MaxStep = 0;
        slopePreprocessing(Slope());
        //exposurePreprocessing(Exposure());
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
//69

        //////////////////////Observaciones realcionadas con el target////////////////////////////////////////
        addObservations(sensor, getInclinationBydirection( transform.forward));
        addObservations(sensor, getInclinationBydirection((transform.forward + transform.right).normalized));
        addObservations(sensor, getInclinationBydirection((transform.forward - transform.right).normalized));
        addObservations(sensor, getInclinationBydirection( transform.right));

        addObservations(sensor, getInclinationBydirection(-transform.right));
        addObservations(sensor, getInclinationBydirection((-transform.forward + transform.right).normalized));
        addObservations(sensor, getInclinationBydirection((-transform.forward - transform.right).normalized));
        addObservations(sensor, getInclinationBydirection(-transform.forward));
        
    }

    public void addObservations(VectorSensor sensor, float [] results) {
        for (int i = 0; i < results.Length; ++i) {           
            sensor.AddObservation(results[i]);
        }
    }
    public void slopePreprocessing(string [][] aux) {
        slope = new float[GRID_SIZE_Z,GRID_SIZE_X];
        for(int i = 0; i < GRID_SIZE_Z; ++i) {
            for(int j = 0; j < GRID_SIZE_X; ++j) {
                slope[i,j] = 90.0f * float.Parse(aux[i][j])/255.0f;
            }
        }
    }

    /*public void exposurePreprocessing(string [][] aux) {
        exposure = new float[GRID_SIZE_Z,GRID_SIZE_X];
        for(int i = 0; i < GRID_SIZE_Z; ++i) {
            for(int j = 0; j < GRID_SIZE_X; ++j) {
                exposure[i,j] = float.Parse(aux[i][j])/255.0f;  // normalizo entre 0 y 1, luego ya en la logica del step haremos * la probabilidad de caída
            }
        }
    }*/

    /*public string [][] Exposure() {
        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes\exposure.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }*/

    public string [][] Slope() {
        var filePath = @"C:\Users\kanga\Documents\TFG\Unity\Modelos\aiguestortes\inclination.csv";
        return File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
    }

    public float getInclination(Vector3 pos) {
        return slope[(int)pos.z,(int)pos.x];
    }

    /*public float getExposure(Vector3 pos) {
        return exposure[(int)pos.z,(int)pos.x];
    }*/

    public float [] getInclinationBydirection(Vector3 direction) { 

        float[] checkPoints = {15f, 60f, 150f, 300f};
        float[] stepSize  = {1.0f, 2.0f, 4.0f, 8.0f};
        Vector3 actualPosition = transform.position;

        //float[] classCount = new float[NUM_CLASSES*checkPoints.Length];
        float [] maxInclinationsByDirection = new float [checkPoints.Length];
        for (int i = 0; i < checkPoints.Length; i++) {

            float maxInclinationByCheckPoint = 0;
            //float maxExposureByCheckPoint = 0;
            float t = i > 0 ? checkPoints[i-1] : 0;
            while (t < checkPoints[i]) {

                // posicion. Mejor calcular siempre para evitar ir acumulando errores precision
                Vector3 pos = transform.position + t*direction;

                // obtenemos clase
                float actualInclination;
                //float actualExposure = getExposure(pos);

                if (!correctPosition(pos)){
                    actualInclination = 1f;
                }

                else {
                    actualInclination = getInclination(pos)/255f;
                }

                // comprobamos si hay que actualizar la max
                if (actualInclination > maxInclinationByCheckPoint) {
                    maxInclinationByCheckPoint = actualInclination;
                }
                /*if (actualExposure > maxExposureByCheckPoint) {
                    maxExposureByCheckPoint = actualExposure;
                }*/
                // avanzamos
                t += stepSize[i];
            }
            maxInclinationsByDirection[i] = maxInclinationByCheckPoint;
            //maxInclinationsByDirection[i+1] = maxExposureByCheckPoint;
        }
        return maxInclinationsByDirection;      
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
            if (correctPosition(targetPos) /*&& !toomuchExposure(targetPos)*/) {
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

            float d = Random.Range(150.0f, 500.0f);
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
            if (correctPosition(agentPos) /*&& !toomuchExposure(agentPos)*/) {
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
    /*
    public bool toomuchExposure(Vector3 pos) {
        float actualExposure = getExposure(pos);
        if (actualExposure > 0.8f) return true;
        else return false;
    }*/
    //Comprueba si una posicion dada esta dentro de los limites del terreno
    public bool correctPosition(Vector3 pos) {

        if (pos.x >= GRID_SIZE_X || pos.x <= 0f) return false;
        if (pos.z >= GRID_SIZE_Z || pos.z <= 0f) return false;

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

            if (distanceToTarget < 30.0f) {  
                AddReward(1.0f);
                Debug.Log("hit at distance " + distanceCovered);
                EndEpisode();      

                target.transform.position = getTargetStartedPosition();   
            }

            /*float actualExposure = getExposure(transform.position);
            if (actualExposure > 0.2f) {
                if (actualExposure> 0.8f) {
                    Debug.Log("fall damage");
                    AddReward(-1.0f);
                    EndEpisode();    
                }
                else{
                    float prob = (actualExposure-0.2f)/0.8f;
                    prob = 0.001f*(prob*prob);
                    //Debug.Log(prob);
                    float r = Random.Range(0f, 1f);
                    if (r < prob) {
                        Debug.Log("fall damage");
                        AddReward(-1.0f);
                        EndEpisode();  
                    }
                }
            }*/

            float actualInclination = getInclination(transform.position);
            //Debug.Log(actualInclination);
            float t = Math.Max(0, (actualInclination-20f)/(90f-20f));
            float mult = 1 + t*t;
            
            AddReward((-1.0f/MAX_WALK_STEPS)*(mult*mult));
            //Debug.Log("Inclination: " + actualInclination);
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
        /*if (collision.gameObject.CompareTag("Target") == true)
        {  
            SetReward(1.5f);
            Debug.Log("hit");
            EndEpisode();         
        }  */
    }
}
  