using UnityEngine;

public class CustomShapeToTilePattern : MonoBehaviour
{
    public static CustomShapeToTilePattern custShapeToPatternInst;

    public TilePattern customPattern;

    void Awake()
    {
        if(custShapeToPatternInst != null && custShapeToPatternInst != this)
        {
            Destroy(gameObject);
            return;
        }

        custShapeToPatternInst = this;
        DontDestroyOnLoad(gameObject);
    }
}
