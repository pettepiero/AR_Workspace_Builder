[System.Serializable]

//Defines serializable data format for relevant properties of GameObjects
public class SaveObject
{
    public string name;
    public float[] position;
    public float[] rotation;
    public float[] scale;
}