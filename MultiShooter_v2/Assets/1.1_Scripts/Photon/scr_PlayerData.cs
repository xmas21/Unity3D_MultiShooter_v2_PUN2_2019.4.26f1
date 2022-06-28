using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class scr_PlayerData : MonoBehaviour
{
    /// <summary>
    /// 儲存檔案
    /// </summary>
    /// <param name="profile">檔案</param>
    public static void SaveProfile(scr_profile profile)
    {
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path)) File.Delete(path);

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, profile);
            file.Close();

            Debug.Log("Save successfully");
        }
        catch
        {
            Debug.Log("Something went wrong");
        }
    }

    /// <summary>
    /// 讀取檔案
    /// </summary>
    /// <returns></returns>
    public static scr_profile LoadProfile()
    {
        scr_profile profile = new scr_profile();

        try
        {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path))
            {
                FileStream fs = File.Open(path, FileMode.Open);

                BinaryFormatter bf = new BinaryFormatter();
                profile = (scr_profile)bf.Deserialize(fs);
                fs.Close();
            }
            Debug.Log("Load successfully");
        }
        catch
        {
            Debug.Log("File does't found");
        }

        return profile;
    }
}
