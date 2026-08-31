using UnityEngine;

// Zona sobe u kojoj dekoracija smije stajati.
public enum DecorZone
{
    Anywhere,     // bilo gdje u sobi
    AgainstWall,  // rubni prsten sobe
    Corner,       // jedan od cetiri kuta
    Center        // unutrasnjost bez rubnog prstena
}

// Jedna vrsta dekoracije s pravilima postavljanja.
// Podatkovna klasa; [System.Serializable] omogucuje uredivanje
// u Inspectoru kao elementa liste na generatoru.
[System.Serializable]
public class DecorationSpec
{
    public GameObject prefab;

    [Tooltip("Gdje u sobi smije stajati")]
    public DecorZone zone = DecorZone.Anywhere;

    [Tooltip("Najmanji broj primjeraka po sobi")]
    public int minPerRoom = 0;

    [Tooltip("Najveci broj primjeraka po sobi")]
    public int maxPerRoom = 2;
}