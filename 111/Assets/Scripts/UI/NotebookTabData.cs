// Assets/Scripts/UI/NotebookTabData.cs
[System.Serializable]
public class NotebookTabData
{
    public string tabName;
    public string content;

    public NotebookTabData(string name)
    {
        tabName = name;
        content = "";
    }
}