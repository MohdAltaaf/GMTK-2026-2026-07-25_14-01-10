using UnityEngine;
using UnityEngine.UI;

public class BombUI : MonoBehaviour
{
    public Bomb bomb;
    Slider slider;
    public GameObject fill;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = bomb.FuseNormalized;
        if (slider.value == 0f) Destroy(fill);
        
    }
}
