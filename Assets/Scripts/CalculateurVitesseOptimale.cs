using UnityEngine;

public static class CalculateurVitesseOptimale
{
    /// <summary>
    /// Calculates optimal circular orbital velocity for a given position around a central body.
    /// </summary>
    /// <param name="position">Position of the orbiting body</param>
    /// <param name="masseCentrale">Mass of the central body</param>
    /// <param name="positionCentrale">Position of the central body</param>
    /// <returns>Orbital velocity vector</returns>
    public static Vector3 CalculerVitesseOrbitaleStatique(Vector3 position, float masseCentrale, Vector3 positionCentrale)
    {
        Vector3 versCentre = positionCentrale - position;
        float distance = versCentre.magnitude;

        if (distance < 0.01f) return Vector3.zero;

        float G = GravityManager.G;
        float multiplicateurGravite = GravityManager.Instance != null ? 
            GravityManager.Instance.gravityMultiplier : 1e13f;

        // Calculate orbital speed: v = sqrt(G * M / r)
        float vitesseOrbitale = Mathf.Sqrt((G * multiplicateurGravite * masseCentrale) / distance);
        
        // Velocity direction: perpendicular to radius, in the XZ plane (reversed)
        Vector3 directionVitesse = Vector3.Cross(Vector3.up, versCentre).normalized;

        return directionVitesse * vitesseOrbitale;
    }
}
