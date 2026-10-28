using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arkesia
{
    public sealed class ArkesiaApp : MonoBehaviour
    {
        public static ArkesiaApp Instance { get; private set; }
        public GameModel Model { get; private set; }
        public BoardWorld World { get; private set; }
        public GameUI UI { get; private set; }
        public GameScreen ScreenId { get; private set; }
        public bool Capturing { get; private set; }
        public string CaptureDirectory { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<ArkesiaApp>() == null) new GameObject("Arkesia · Native Unity Application").AddComponent<ArkesiaApp>();
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Model = new GameModel();
            AudioListener.volume = Model.profile.muted ? 0 : 1;
            if (EventSystem.current == null)
            {
                var input = new GameObject("UI Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                input.transform.SetParent(transform);
            }
            World = gameObject.AddComponent<BoardWorld>(); World.Initialize(Model, InspectTile);
            UI = gameObject.AddComponent<GameUI>(); UI.Initialize(this);
            Show(GameScreen.Home);
        }

        public void Show(GameScreen id)
        {
            if (World.moving) return;
            ScreenId = id;
            if (id == GameScreen.Battle) World.ShowBattle(); else World.ShowBoard();
            World.interact = id == GameScreen.Board;
            World.Refresh(); UI.Draw(id);
        }
        public void Refresh() { World.Refresh(); UI.Draw(ScreenId); }
        public void InspectTile(int index)
        {
            if (World.moving || index < 0 || index >= 32) return;
            Model.selectedTile = index;
            if (GameData.Prices[index] > 0) Show(GameScreen.Property);
            else if (GameData.IsFate(index)) { Model.NewFate(); Show(GameScreen.Fate); }
            else UI.Toast(GameData.Tiles[index] + " · 특수 칸 효과는 개발 예정입니다.");
        }
        public void Roll()
        {
            if (World.moving || ScreenId != GameScreen.Board) return;
            StartCoroutine(MovePawn());
        }
        IEnumerator MovePawn()
        {
            World.moving = true;
            Model.dieA = UnityEngine.Random.Range(1, 7); Model.dieB = UnityEngine.Random.Range(1, 7);
            UI.Draw(ScreenId);
            int count = Model.dieA + Model.dieB;
            for (int i = 0; i < count; i++)
            {
                int next = (Model.position + 1) % 32;
                if (next == 0) Model.money += 200;
                Vector3 from = World.pawns[0].position, to = World.tilePoints[next];
                Vector3 direction = to - from;
                if (direction.sqrMagnitude > .01f) World.pawns[0].rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                float duration = Model.profile.reducedMotion ? .02f : .22f;
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    float f = Mathf.Clamp01(t / duration);
                    World.pawns[0].position = Vector3.Lerp(from, to, f) + Vector3.up * (Model.profile.reducedMotion ? 0 : Mathf.Sin(f * Mathf.PI) * .24f);
                    yield return null;
                }
                World.pawns[0].position = to; Model.position = next;
            }
            Model.turn++; World.moving = false; UI.Draw(ScreenId);
            // Deliberately local demo transitions. No server authority or reward settlement is claimed.
            if (Model.position == 9 || Model.position == 17 || Model.position == 25)
            { Model.ResetBattle(); Show(GameScreen.Battle); }
            else InspectTile(Model.position);
        }
        public void Purchase()
        {
            if (!Model.BuyOrBuild(Model.selectedTile)) { UI.Toast("구매할 수 없습니다."); return; }
            Show(GameScreen.Board); UI.Toast(GameData.Tiles[Model.selectedTile] + " · 구매 / 건설 완료");
        }
        public void CaptureAll()
        {
            if (Capturing || World.moving) return;
            StartCoroutine(CaptureScreens());
        }
        IEnumerator CaptureScreens()
        {
            Capturing = true; GameScreen previous = ScreenId;
            CaptureDirectory = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures"))
                : Path.Combine(Application.persistentDataPath, "Captures");
            Directory.CreateDirectory(CaptureDirectory);
            for (int i = 0; i < GameData.Screens.Length; i++)
            {
                Show((GameScreen)i);
                yield return null;
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(CaptureDirectory, "M-" + (i + 1).ToString("000") + ".png");
                // Encode the framebuffer synchronously so the next UI cannot race the capture.
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                byte[] png = shot.EncodeToPNG(); Destroy(shot); File.WriteAllBytes(path, png);
            }
            Show(previous); Capturing = false;
            Debug.Log("Arkesia: 11 actual Unity screenshots saved to " + CaptureDirectory);
            UI.Toast("11개 실행 화면 캡처 완료 · Captures 폴더");
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) CaptureAll();
            if (Input.GetKeyDown(KeyCode.Escape) && !Capturing) Show(GameScreen.Home);
        }
        void OnApplicationPause(bool paused) { if (paused && Model != null) Model.Save(); }
        void OnApplicationQuit() { if (Model != null) Model.Save(); }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
