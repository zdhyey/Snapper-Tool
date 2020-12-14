using UnityEngine;
using UnityEditor;

public class SnapperTool : EditorWindow
{

    public enum GridType{
        Cartesian,
        Polar
    }

    [MenuItem("My Tools/Snapper")]
    public static void OpenTheThing() => GetWindow<SnapperTool>("Snapper");

    const float TAU = 6.28318530718f;

    public float gridSize = 1f;
    public GridType gridType = GridType.Cartesian;
    public int angularDivisions = 24;


    SerializedObject so;
    SerializedProperty propGridSize;
    SerializedProperty propGridType;
    SerializedProperty propAngularDivisions;

    //public Vector3[] points = new Vector3[4];
    //SerializedProperty propPoints;
    
    void OnEnable()
    {
        so = new SerializedObject(this);
        propGridSize = so.FindProperty("gridSize");
        propGridType = so.FindProperty("gridType");
        propAngularDivisions = so.FindProperty("angularDivisions");
        //propPoints = so.FindProperty("points");

        //load saved configurations
        gridSize = EditorPrefs.GetFloat("SNAPPER_TOOL_gridSize", 1f);
        gridType = (GridType)EditorPrefs.GetInt("SNAPPER_TOOL_gridType", 0);
        angularDivisions = EditorPrefs.GetInt("SNAPPER_TOOL_angularDivisions", 24);
        Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable()
    {
        // save configurations
        EditorPrefs.SetFloat("SNAPPER_TOOL_gridSize", gridSize);
        EditorPrefs.SetInt("SNAPPER_TOOL_gridType", (int)gridType);
        EditorPrefs.SetInt("SNAPPER_TOOL_angularDivisions", angularDivisions);
        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        /*
        // Array Shenanigans
        so.Update();
        for(int i = 0;i < propPoints.arraySize; i++)
        {
            SerializedProperty prop = propPoints.GetArrayElementAtIndex(i);
            prop.vector3Value = Handles.PositionHandle(prop.vector3Value, Quaternion.identity);
        }
        so.ApplyModifiedProperties();
        */

        if(Event.current.type == EventType.Repaint)
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            const float gridDrawExtent = 16;

            if(gridType == GridType.Cartesian)
            {
                DrawGridCartesian(gridDrawExtent);
            }
            else
            {
                DrawGridPolar(gridDrawExtent);
            }
            
        }
        
    } 

    

    void DrawGridPolar(float gridDrawExtent)
    {
        int ringCount = Mathf.RoundToInt(gridDrawExtent / gridSize);

        float radiusOuter = (ringCount-1) * gridSize;
        //rings
        for(int i = 1; i < ringCount; i++)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, i * gridSize);
        }

        
        // angluar grids
        for(int i = 0; i < angularDivisions; i++)
        {
            float t = i/(float)angularDivisions;
            float angRad = t * TAU; //turn to radians
            float x = Mathf.Cos(angRad);
            float y = Mathf.Sin(angRad);
            Vector3 dir = new Vector3(x, 0f, y);

            Handles.DrawAAPolyLine(Vector3.zero, dir * radiusOuter);
        }
    }

    void DrawGridCartesian(float gridDrawExtent)
    {
        int lineCount = Mathf.RoundToInt((gridDrawExtent * 2)/gridSize);
        if(lineCount%2 == 0)
        {   
            lineCount++;  //to make sure thet lineCount is odd
        }
        int halfLineCount = lineCount/2;
        for(int i = 0; i < lineCount; i++)
        {
            int intOffset = i - halfLineCount;

            float xCoord = intOffset * gridSize;
            float zCoord0 = halfLineCount * gridSize;
            float zCoord1 = -halfLineCount * gridSize;

            Vector3 p0 = new Vector3(xCoord, 0f, zCoord0 );
            Vector3 p1 = new Vector3(xCoord, 0f, zCoord1 ); 

            Handles.DrawAAPolyLine(p0, p1); 

            p0 = new Vector3(zCoord0, 0f, xCoord);
            p1 = new Vector3(zCoord1, 0f, xCoord); 

            Handles.DrawAAPolyLine(p0, p1); 
        }
    }


    void OnGUI()
    {   
        so.Update();
        EditorGUILayout.PropertyField(propGridType);
        EditorGUILayout.PropertyField(propGridSize);
        if(gridType == GridType.Polar)
        {
            EditorGUILayout.PropertyField(propAngularDivisions);
            propAngularDivisions.intValue = Mathf.Max(4, propAngularDivisions.intValue);
        }
        so.ApplyModifiedProperties();
        
        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if(GUILayout.Button("Snap Selection"))
            {
                SnapSlelction();  
            }

        }
    
    }


    void SnapSlelction()
    {
        foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RecordObject(go.transform,"snap objects"); 
                go.transform.position = GetSnappedPosition(go.transform.position); //go.transform.position.Round(gridSize);
            }
    }


    Vector3 GetSnappedPosition(Vector3 posOriginal)
    {
        if(gridType == GridType.Cartesian)
        {
            return posOriginal.Round(gridSize);
        }

        if(gridType == GridType.Polar)
        {
            Vector2 vec = new Vector2(posOriginal.x, posOriginal.z);
            float dist = vec.magnitude; 
            float distSnapped = dist.Round(gridSize);
            
            float angRad = Mathf.Atan2(vec.y, vec.x);
            float angTurns = angRad/TAU;
            float angTurnsSnapped = angTurns.Round(1f/angularDivisions);
            float angRadSnapped = angTurnsSnapped * TAU;

            Vector2 dirSnapped = new Vector2(Mathf.Cos(angRadSnapped), Mathf.Sin(angRadSnapped));
            Vector2 snappedVec = dirSnapped * distSnapped;

            return new Vector3(snappedVec.x, posOriginal.y, snappedVec.y);
        }  

        return default; 
    }
}
