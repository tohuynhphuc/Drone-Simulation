using UnityEngine;

public class Manager : MonoBehaviour {
    public static Manager Instance { get; private set; }

    [SerializeField] private bool useROS = true;

    public bool UseROS {
        get {
            return useROS;
        }
        set {
            useROS = value;
        }
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }
}
