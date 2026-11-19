using System.Collections.Generic;
using UnityEngine;

// Create scriptable object for certain patterns, so they can easily be used, reused, and created
// TODO: Later have a "Tile Pattern Creator" function
[CreateAssetMenu(menuName = "Cellular Automatata/Pattern")]
public class TilePattern : ScriptableObject
{
    // Storing the cells of the grid as integer based Vector2 array elements for coordinates like (x,y)
    // Pattern will be stored in a grid of 5 cells - a Pentomino (like a tetromino in Tetris)
    // This is due to prior research indicating the sweet spot as the smallest shapes to produce complexity e.g. the R-Pentomino (Conway highlighted; studied by Bill Gosper; see Glider Gun)
    // Pentomino - https://mathworld.wolfram.com/Pentomino.html
    // More 1 - https://mathworld.wolfram.com/CellularAutomaton.html
    // More 2 - https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
    
    public Vector2Int[] patternGridCells;

    // Returns the center location of the pattern based on its shape
    public Vector2Int GetCenterOfPatternForOffset()
    {
        if(patternGridCells == null || patternGridCells.Length == 0)
        {
            return Vector2Int.zero;
        }

        // Get the bounds of all cells
        Vector2Int minBounds = Vector2Int.zero;
        Vector2Int maxBounds = Vector2Int.zero;

        // Loop over all cells to find the extent 
        for(int i = 0; i < patternGridCells.Length; i++)
        {
            Vector2Int currentCell = patternGridCells[i];

            // Compare and return the smaller of two values and set that as the new min
            minBounds.x = Mathf.Min(currentCell.x, minBounds.x);
            minBounds.y = Mathf.Min(currentCell.y, minBounds.y);

            // Compare and return the larger of two values and set that as the new max
            maxBounds.x = Mathf.Max(currentCell.x, maxBounds.x);
            maxBounds.y = Mathf.Max(currentCell.y, maxBounds.y);
        }

        return (minBounds + maxBounds)/2;

    }
}
