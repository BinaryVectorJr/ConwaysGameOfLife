using System.IO;
using System.Text;
using UnityEngine;

public class WriteToFile : MonoBehaviour
{
    public static WriteToFile filewriterInstance { get; private set; }

    string fileName;

    // Lock object (multithreaded implementation later)
    private readonly object fileLock = new object();

    void Awake()
    {
        if(filewriterInstance != null && filewriterInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        filewriterInstance = this;
        DontDestroyOnLoad(gameObject);

    }

    void Start()
    {
        fileName = MainSimulation.mainInstance.recordName;
    }

    public void WriteDataToFile(string _dir, int _agentType, int _x, int _y, string _val, string _mode)
    {
        lock(fileLock)
        {
            if(!Directory.Exists(_dir))
            {
                Directory.CreateDirectory(_dir);
            }

            string fullFilePath = Path.Combine(_dir, fileName + "-" + _mode + ".txt");

            string data = $"{_agentType},{_x},{_y},{_val}\n";

            // Append multiple data
            File.AppendAllText(fullFilePath, data, Encoding.UTF8);
        }
    }

    public void WriteIterationsToFile(string _dir, int _step, int _unsatagents)
    {
        lock(fileLock)
        {
            if(!Directory.Exists(_dir))
            {
                Directory.CreateDirectory(_dir);
            }

            string fullFilePath = Path.Combine(_dir, fileName + "- ITERS" + ".txt");

            string data = $"{_step},{_unsatagents}\n";

            // Append multiple data
            File.AppendAllText(fullFilePath, data, Encoding.UTF8);
        }
    }


}
