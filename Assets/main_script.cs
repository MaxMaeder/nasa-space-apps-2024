using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main_script : MonoBehaviour
{
    //Public variables used for initialization
    public ParticleSystem particle_system_prefab;
    public GameObject main_ui_prefab;
//    public int star_count = 118218;
    public Camera camera_prefab;
    public TextAsset star_data;
    public TextAsset exoplanet_data;
    public Material clicked_material;
    public Material unclicked_material;

    //private variables used internally
    private UIScript ui_script;
    private Camera camera;
    public GameObject planet_prefab;
    public GameObject collider_prefab;
    private ParticleSystem particle_system;
    private ParticleSystem.Particle[] particles;
    private List<GameObject> particle_colliders = new List<GameObject>();
    private Vector3 look;
    public bool constellationMode = false;
    private float planet_camera_distance;
    private Vector3 last_location;
    private Vector3 next_location;
    private planet_script clicked_planet;
    private float percent_travelled;
    private float percent_travelled_plus;
    private float travel_seconds;
    private float travel_plus_seconds;
    private float travel_power;
    private int distance_multiplier;
    public bool show_planet_UI = true;
    private float lookSpeed;
    private float size_of_earth;
    private planet_script earth_script;
    private float clicked_planet_size;
    private int planet_count_initialized;
    private GameObject[] planets;
    private float planet_UI_size;
    private Quaternion next_angle;
    private Quaternion angle_when_clicked;
    private Vector3 camera_default_angle;
    private bool locked_screen;
    private Vector3 next_planet_location;

    // Start is called before the first frame update
    void Start()
    {
        initialize_globals();
        initialize_UI();
        initialize_camera();

        initialize_particles();
        initialize_planets();
    }

    private Vector3[] get_locations(int num_locations, int min_val, int max_val) {
        //{new Vector3(0, 0, 1), new Vector3(1, 0, 0)};
        Vector3[] new_locations = new Vector3[num_locations];
        for(int i = 0; i < num_locations; i ++) {
            new_locations[i] = new Vector3(Random.Range(min_val, max_val), Random.Range(min_val, max_val), Random.Range(min_val, max_val));
        }
        return new_locations;
    }

    // Update is called once per frame
    void Update() {
        update_planets();
        update_clicked_planet();
        update_camera(); //This must be called after update_clicked_planet
        update_commands();
    }

    private void update_commands() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            constellationMode = !constellationMode;
            if (constellationMode) {
                set_planet_visibility(false);
                Cursor.lockState = CursorLockMode.None;
            } else {
                set_planet_visibility(true);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (Input.GetKeyDown(KeyCode.H) && !constellationMode && !locked_screen) {
            moveCamera(new Vector3(0, 0, 0), earth_script);
        }

        if(!locked_screen) {
            float orbitSpeed = 100f;
            Vector3 offset = (camera.transform.position - next_planet_location).normalized * planet_camera_distance;

            float horizontalInput = Input.GetAxis("Horizontal"); // Left (-1) and Right (+1) arrow keys
            float verticalInput = Input.GetAxis("Vertical"); // Up (+1) and Down (-1) arrow keys

            // Calculate the rotation around the Y axis (sideways orbit)
            if (horizontalInput != 0)
            {
                Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalInput * orbitSpeed * Time.deltaTime, Vector3.up);
                offset = horizontalRotation * offset;
            }

            // Calculate the rotation around the X axis (forward/backward orbit)
            if (verticalInput != 0)
            {
                Quaternion verticalRotation = Quaternion.AngleAxis(verticalInput * orbitSpeed * Time.deltaTime, transform.right);
                offset = verticalRotation * offset;
            }
            camera.transform.position = next_planet_location + offset;
        }

    }

    private void initialize_particles() {
        int total_particle_count = 0;
        CSVParser.Parse(star_data, 0, 2, (location) => {
            total_particle_count ++;
        });
        particle_system = Instantiate(particle_system_prefab);
        particle_system.Emit(total_particle_count);
        particles = new ParticleSystem.Particle[total_particle_count];
        particle_system.GetParticles(particles);
        int particle_count = 0;
        float size;
        CSVParser.Parse(star_data, 0, 6, (location) => {
          Vector3 position = new Vector3(location[0] * distance_multiplier, location[1] * distance_multiplier, location[2] * distance_multiplier);
            particles[particle_count].position = position;
            particles[particle_count].startColor = new Color(location[4], location[5], location[6]);
            particles[particle_count].size = location[3];
            particle_count++;
        });
        particle_system.SetParticles(particles);

        create_colliders_for_particles();
        
    }

        // Create a collider for each particle
    private void create_colliders_for_particles()
    {
        // Loop through particles and create colliders at their positions
        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 position = particles[i].position;

            // Instantiate a collider GameObject at the particle's position
            GameObject colliderObject = Instantiate(collider_prefab, position, Quaternion.identity);
            colliderObject.transform.localScale = Vector3.one * 1f; // Set collider size, tweak as needed

            // Add a tag to easily identify the colliders later
            colliderObject.tag = "StarCollider";

            // Store collider reference for later manipulation if needed
            particle_colliders.Add(colliderObject);
        }
    }

    private void initialize_planets() {
        int planet_count_max = 1;
        CSVParser.Parse(exoplanet_data, 1, 3, (location) => {
            planet_count_max ++;
        });
        planets = new GameObject[planet_count_max];
        planet_UI_size = 0.015f;
        planet_count_initialized = 0;
        List<Dictionary<string,object>> planet_data = CSVReader.Read(exoplanet_data);
        foreach(Dictionary<string,object> location in planet_data)
        {
            if (location["X"] is float xValue && location["Y"] is float yValue && location["Z"] is float zValue)
            {
                GameObject planet = Instantiate(planet_prefab);
                planet.transform.position = new Vector3((float)location["X"] * distance_multiplier, (float)location["Y"] * distance_multiplier, (float)location["Z"] * distance_multiplier);
                planet.GetComponent<planet_script>().planet_name = (string)location["Planet Name"];
                planet.GetComponent<planet_script>().main = this;
                planet.GetComponent<planet_script>().main_ui = ui_script;
                planets[planet_count_initialized] = planet;//new Vector3(location[1], location[2], location[3]);
                planet_count_initialized += 1;
            }

            planet_UI_size = 0.015f;
        }

        //Create Earth
        GameObject my_planet = Instantiate(planet_prefab);
        my_planet.transform.position = new Vector3(0,0,0);
        my_planet.GetComponent<planet_script>().main = this;
        next_planet_location = new Vector3(0,0,0);
        my_planet.GetComponent<planet_script>().main_ui = ui_script;
        my_planet.GetComponent<MeshRenderer>().material = clicked_material;
        clicked_planet = my_planet.GetComponent<planet_script>();
        earth_script = my_planet.GetComponent<planet_script>();
        planets[planet_count_initialized] = my_planet;
        planet_count_initialized ++;
    }
    private void initialize_globals() {
        distance_multiplier = 5; //2000
        size_of_earth = 0.000000002f;
        clicked_planet_size = 0.02f;
//        Cursor.lockState = CursorLockMode.Locked;
    }
    private void initialize_UI() {
        ui_script = Instantiate(main_ui_prefab).GetComponent<UIScript>();
    }
    private void initialize_camera() {
        locked_screen = false;
        camera_default_angle = new Vector3(270, 0, 0);
        camera = Instantiate(camera_prefab);
        Vector3 look = camera_default_angle;
        planet_camera_distance = clicked_planet_size * distance_multiplier / 2 + 0.0100000001f;
        camera.transform.position = new Vector3(0, planet_camera_distance, 0);
        next_location = new Vector3(0, planet_camera_distance, 0);
        percent_travelled = 1;
        percent_travelled_plus = 1;
        travel_seconds = 4f;
        travel_plus_seconds = 0.75f;
        travel_power = 2f;
        lookSpeed = 2f;

        Cursor.lockState = CursorLockMode.Locked;
    }
    private void update_camera() {
        if(percent_travelled != 1) {
            if (constellationMode) {
                constellationMode = false;
            }
            percent_travelled = Mathf.Min(1, percent_travelled + Time.deltaTime / travel_seconds);
            float eased_percent_travelled = Mathf.Pow((1 - Mathf.Cos(percent_travelled * Mathf.PI))/ 2, travel_power);
            camera.transform.position = Vector3.Lerp(last_location, next_location, eased_percent_travelled);
        } else if(percent_travelled_plus != 1) {
            percent_travelled_plus = Mathf.Min(1, percent_travelled_plus + Time.deltaTime / travel_plus_seconds);
            Quaternion lerping_quaternion = Quaternion.Lerp(angle_when_clicked, next_angle, percent_travelled_plus);
            look = lerping_quaternion.eulerAngles;
            if(percent_travelled_plus == 1) {
                locked_screen = false;
                show_planet_UI = true;
                look = camera.transform.eulerAngles;
            }
        } else {
            if (!constellationMode) {
                look = look + new Vector3(-Input.GetAxis("Mouse Y") * lookSpeed, Input.GetAxis("Mouse X") * lookSpeed, 0);
            }
        }
        camera.transform.eulerAngles = look;
    }

    public void moveCamera(Vector3 new_position, planet_script planet_scr) {
        if(clicked_planet != planet_scr) {
            clicked_planet.GetComponent<MeshRenderer>().material = unclicked_material;
            last_location = camera.transform.position;
            clicked_planet = planet_scr;
            percent_travelled = 0;
            percent_travelled_plus = 0;
            locked_screen = true;
            clicked_planet.GetComponent<MeshRenderer>().material = clicked_material;

            //Get angle to target
            Vector3 directionToTarget = clicked_planet.transform.position - camera.transform.position;
            Vector3 normalized_direction_to_target = Vector3.Normalize(directionToTarget);
            Vector3 relativePos = clicked_planet.transform.position - camera.transform.position;
            Quaternion rotation = Quaternion.LookRotation(relativePos, camera.transform.up);
            angle_when_clicked = Quaternion.Euler(camera.transform.eulerAngles);
            next_angle = rotation * Quaternion.Euler(0, 180, 0);
            Vector3 planet_offset = normalized_direction_to_target * planet_camera_distance;
            next_planet_location = new_position;
            if(Vector3.Distance(camera.transform.position, new_position + planet_offset) >
                Vector3.Distance(camera.transform.position, new_position - planet_offset)) {
                next_location = new_position - planet_offset;
                } else {
                    next_location = new_position + planet_offset;
                }

            show_planet_UI = false;
        }
    }

    private Vector3 standardize_angle(Vector3 vec) {
        while(vec.x > 180) {
            vec = vec + new Vector3(-180, 0, 0);
        }
        while(vec.x < 0) {
            vec = vec + new Vector3(180, 0, 0);
        }
        while(vec.y > 180) {
            vec = vec + new Vector3(0, -180, 0);
        }
        while(vec.y < 0) {
            vec = vec + new Vector3(0, 180, 0);
        }
        while(vec.z > 180) {
            vec = vec + new Vector3(0, 0, -180);
        }
        while(vec.z < 0) {
            vec = vec + new Vector3(0, 0, 180);
        }
        return vec;
    }

    public void update_planets() {
        for(int i = 0; i < planet_count_initialized; i ++) {
            // Get the distance between the planet and the camera
            float distance = Vector3.Distance(planets[i].transform.position, camera.transform.position);

            // Adjust the scale based on the distance
            float scale;
            if(show_planet_UI) {
                scale = distance * planet_UI_size;
            } else {
                scale = size_of_earth;
            }
            planets[i].transform.localScale = new Vector3(scale, scale, scale);
        }
    }
    public void update_clicked_planet() {
        if(clicked_planet) {
            float distance = Vector3.Distance(clicked_planet.transform.position, camera.transform.position);
            float scale = clicked_planet_size * distance_multiplier;
            clicked_planet.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    private void set_planet_visibility(bool visible) {
        show_planet_UI = visible;
    }
}