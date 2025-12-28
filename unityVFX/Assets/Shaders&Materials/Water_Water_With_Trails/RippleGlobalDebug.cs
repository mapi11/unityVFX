using UnityEngine;

public class RippleGlobalDebug : MonoBehaviour
{
    void Update()
    {
        if (Time.frameCount % 30 != 0) return;

        var a = Shader.GetGlobalTexture("_RippleRT");
        var b = Shader.GetGlobalTexture("_RipleRT");

        Debug.Log(a ? $"Global _RippleRT = {a.name} ({a.GetType().Name})" : "Global _RippleRT = NULL");
        Debug.Log(b ? $"Global _RipleRT  = {b.name} ({b.GetType().Name})" : "Global _RipleRT  = NULL");
    }
}
