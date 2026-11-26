using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Gameplay
{
    // Port-local geometry keeps the clinic and patrol aligned with Varo in both reference frames.
    public class VaroClinic : MonoBehaviour
    {
        private Transform medic, guard;
        private PlayerController player;
        private float suspicion;
        private Vector3 previousPatrol;
        private static readonly Vector3 ClinicDesk = new Vector3(5, 1, -32);
        private static readonly Vector3 GateDesk = new Vector3(7, 1, -21);

        public static void Install(Transform ring)
        {
            var wing = new GameObject("Varo — klinika i przejście techniczne");
            wing.transform.SetParent(ring, false);
            wing.transform.localPosition = Vector3.up * 13;
            var clinic = wing.AddComponent<VaroClinic>();
            clinic.player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            var fixture = new GameObject("Oświetlenie ambulatorium");
            fixture.transform.SetParent(wing.transform, false);
            fixture.transform.localPosition = new Vector3(0, 5, -27);
            var light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 25;
            light.intensity = 5;
            light.color = new Color(0.75f, 0.88f, 1f);
            var deck = ring.Find("Pokład").GetComponent<Renderer>().sharedMaterial;
            var rail = ring.Find("Barierka_S");
            var wall = rail.GetComponent<Renderer>().sharedMaterial;
            rail.gameObject.SetActive(false);
            var lamp = ring.Find("Lądowisko").GetComponent<Renderer>().sharedMaterial;

            clinic.Box("Podłoga kliniki", 0,-0.5f,-26,32,1,20,deck);
            clinic.Box("Ściana zachodnia",-16,2,-26,0.4f,4,20,wall);
            clinic.Box("Ściana wschodnia",16,2,-26,0.4f,4,20,wall);
            clinic.Box("Ściana tylna",0,2,-36,32,4,0.4f,wall);
            clinic.Box("Parapet okna serwisu",3,0.65f,-24,26,1.3f,0.4f,wall);
            clinic.Box("Rama okna",3,3.8f,-24,26,0.4f,0.4f,wall);
            var pane = clinic.Box("Szyba",3,2.5f,-24,26,2.6f,0.2f,wall);
            pane.GetComponent<Renderer>().enabled = false;
            clinic.Box("Bramka kontroli",7,1.35f,-24,2.4f,2.7f,0.6f,wall);
            var intercom = clinic.Box("Interkom",7,1.3f,-23.4f,0.7f,0.8f,0.45f,lamp);
            intercom.AddComponent<ClinicContact>().role = ClinicContact.Role.Intercom;
            clinic.Sign("KLINIKA   +", 5, 3, -33);
            clinic.Sign("KONTROLA   /   INTERKOM", 7, 3, -23.5f);
            clinic.Sign("TECHNICZNE", -12, 2.7f, -20);
            clinic.Box("Osłona serwisowa",-12,0.65f,-26,2,1.3f,1,wall);
            clinic.Box("Osłona przewodów",-8,0.65f,-30,2,1.3f,1,wall);
            clinic.medic = clinic.Person("Medyczka", ClinicDesk, lamp);
            clinic.medic.gameObject.AddComponent<ClinicContact>().role = ClinicContact.Role.Medic;
            clinic.previousPatrol = new Vector3(-13,1,-32);
            clinic.guard = clinic.Person("Patrol", clinic.previousPatrol, wall);
            var visor = clinic.Box("Wizjer patrolu",0,0,0,0.64f,0.18f,0.18f,lamp);
            visor.transform.SetParent(clinic.guard, false);
            visor.transform.localPosition = new Vector3(0,0.55f,0.43f);
            clinic.Sign("PATROL", 0, 1.5f, 0, clinic.guard);
        }

        private GameObject Box(string label, float x,float y,float z,float w,float h,float d, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = label;
            box.transform.SetParent(transform, false);
            box.transform.localPosition = new Vector3(x,y,z);
            box.transform.localScale = new Vector3(w,h,d);
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private Transform Person(string label, Vector3 position, Material material)
        {
            var person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.name = label;
            person.transform.SetParent(transform, false);
            person.transform.localPosition = position;
            person.GetComponent<Renderer>().sharedMaterial = material;
            return person.transform;
        }

        private void Sign(string text, float x,float y,float z, Transform parent = null)
        {
            var sign = new GameObject(text);
            sign.transform.SetParent(parent == null ? transform : parent, false);
            sign.transform.localPosition = new Vector3(x,y,z);
            sign.transform.localRotation = Quaternion.Euler(0,180,0);
            var mesh = sign.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.075f;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
        }

        private void Update()
        {
            if (GameState.Mission == GameState.Stage.Outbound) medic.localPosition = ClinicDesk;
            if (GameState.Mission == GameState.Stage.Waiting)
            {
                // Staff use the same opening west of the inspection window.
                float progress = Mathf.Clamp01((float)(1 - GameState.ApprovalRemaining / 25));
                Vector3 insideDoor = new Vector3(-14,1,-30);
                Vector3 outsideDoor = new Vector3(-14,1,-21);
                if (progress < 0.4f) medic.localPosition = Vector3.Lerp(ClinicDesk, insideDoor, progress / 0.4f);
                else if (progress < 0.6f) medic.localPosition = Vector3.Lerp(insideDoor, outsideDoor, (progress - 0.4f) / 0.2f);
                else medic.localPosition = Vector3.Lerp(outsideDoor, GateDesk, (progress - 0.6f) / 0.4f);
            }
            float x = -13 + Mathf.PingPong(Time.time * 2, 10);
            var point = new Vector3(x,1,-32);
            var direction = point - previousPatrol;
            if (direction.sqrMagnitude > 0.000001f) guard.localRotation = Quaternion.LookRotation(direction);
            guard.localPosition = previousPatrol = point;

            if (!GameState.AtClinic && !(GameState.Mission == GameState.Stage.Return &&
                GameState.Package == GameState.Medicine.Undeclared)) return;
            if (player == null || !player.isActiveAndEnabled) return;
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            if (local.z >= -24 || local.z < -36 || Mathf.Abs(local.x) > 16 || Mathf.Abs(local.y) > 3)
            { suspicion = 0; return; }
            Vector3 eye = guard.position + transform.up * 0.7f;
            Vector3 ray = player.transform.position + transform.up * player.EyeHeight - eye;
            bool visible = ray.sqrMagnitude < 110 && Vector3.Dot(guard.forward, ray.normalized) > 0.25f &&
                Physics.Raycast(eye, ray.normalized, out var hit, ray.magnitude + 0.4f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<PlayerController>() == player;
            // Footsteps in the restricted corridor carry around a guard facing away.
            Vector3 speed = player.GetComponent<CharacterController>().velocity;
            bool running = !player.IsCrouched && Vector3.ProjectOnPlane(speed, transform.up).sqrMagnitude > 16f;
            bool audible = running && ray.sqrMagnitude < 196f;
            float before = suspicion;
            suspicion = audible ? 1f : Mathf.Clamp01(suspicion + Time.deltaTime * (visible ? (player.IsCrouched ? 0.45f : 3f) : -1));
            if (before < 0.25f && suspicion >= 0.25f) GameState.Notify("Patrol cię obserwuje. Schowaj się za osłoną.");
            if (suspicion < 1) return;
            bool confiscated = GameState.Mission == GameState.Stage.Return;
            if (confiscated) GameState.ConfiscateMedicine();
            player.TeleportTo(transform.TransformPoint(new Vector3(-9,0.1f,-20)), transform.rotation);
            suspicion = 0;
            GameState.Notify(confiscated ? "Patrol odebrał lek i odprowadził cię do magazynu. Wróć na Kesh z modułem." : "Patrol odprowadził cię do magazynu. Obserwuj strażnika i kucaj za osłonami [C].");
        }
    }
}
