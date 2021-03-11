using Unity.Entities;
using UnityEngine;

public class ControllableCamera : MonoBehaviour {

    public static ControllableCamera instance;

    public void Awake() {
        instance = this;
    }
}
