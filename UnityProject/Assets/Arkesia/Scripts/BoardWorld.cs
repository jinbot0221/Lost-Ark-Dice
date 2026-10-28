using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Arkesia
{
    public sealed class TileHit : MonoBehaviour { public int index; }

    /// <summary>Native Unity meshes, lighting and depth. No browser or embedded WebView.</summary>
    public sealed class BoardWorld : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public readonly Vector3[] tilePoints = new Vector3[32];
        public readonly Transform[] pawns = new Transform[4];
        public readonly RenderTexture[] portraits = new RenderTexture[6];
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<GameObject> buildings = new List<GameObject>();
        Transform board, arena;
        GameModel model;
        Action<int> onTile;
        public bool interact, moving;
        public bool ReducedMotion { get; set; }
        float azimuth = 38, zoom = 12.4f;
        Vector2 press, previousTouch;
        bool dragging, acceptedPress;
        bool landscape;
        int heroId;
        Light sunlight;

        public void Initialize(GameModel game, Action<int> selected)
        {
            model = game; onTile = selected;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("DAE9E4");
            RenderSettings.ambientEquatorColor = Hex("A8C6AB");
            RenderSettings.ambientGroundColor = Hex("6F8E74");
            RenderSettings.fog = true; RenderSettings.fogColor = Hex("C5DCE0");
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 32; RenderSettings.fogEndDistance = 90;
            Camera = new GameObject("World Camera").AddComponent<Camera>();
            Camera.tag = "MainCamera"; Camera.orthographic = true; Camera.nearClipPlane = .1f; Camera.farClipPlane = 160;
            Camera.backgroundColor = Hex("C5DCE0"); Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.cullingMask = ~(1 << 8); Camera.allowHDR = false;
            sunlight = new GameObject("Warm afternoon light").AddComponent<Light>();
            sunlight.type = LightType.Directional; sunlight.color = Hex("FFF0CF"); sunlight.intensity = 1.35f;
            sunlight.transform.rotation = Quaternion.Euler(43, -35, 0);
            sunlight.shadows = LightShadows.Soft; sunlight.shadowStrength = .65f; sunlight.shadowBias = .04f;
            QualitySettings.shadowDistance = 55; QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium; QualitySettings.antiAliasing = 4;
            board = new GameObject("32-tile floating garden").transform;
            BuildGarden(); BuildPath(); BuildClouds();
            int[] positions = { game.position, 9, 17, 25 };
            for (int i = 0; i < 4; i++)
            {
                pawns[i] = MakeHero(i == 0 ? game.profile.representative : i, board);
                pawns[i].name = "Player " + (i + 1) + " standing on tile";
                pawns[i].position = tilePoints[positions[i]];
                MakeRing(pawns[i], i == 0 ? "EFD080" : i == 1 ? "7ABED7" : i == 2 ? "D998AA" : "9EBF7B");
            }
            heroId = game.profile.representative;
            MakePortraits(); BuildArena(); ShowBoard();
        }

        public static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
        Material Mat(string color)
        {
            Material m;
            if (!materials.TryGetValue(color, out m))
            {
                Material template = Resources.Load<Material>("PrototypeStandard");
                m = template ? new Material(template) : new Material(Shader.Find("Standard")); m.color = Hex(color);
                m.SetFloat("_Glossiness", .18f); materials.Add(color, m);
            }
            return m;
        }
        Transform Shape(PrimitiveType type, string name, Vector3 p, Vector3 s, string color, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = s;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            var c = go.GetComponent<Collider>(); if (c) { c.enabled = false; Destroy(c); }
            return go.transform;
        }
        Transform Box(string n, Vector3 p, Vector3 s, string c, Transform t) { return Shape(PrimitiveType.Cube, n, p, s, c, t); }
        Transform Ball(string n, Vector3 p, Vector3 s, string c, Transform t) { return Shape(PrimitiveType.Sphere, n, p, s, c, t); }
        Transform Cylinder(string n, Vector3 p, Vector3 s, string c, Transform t) { return Shape(PrimitiveType.Cylinder, n, p, s, c, t); }

        void BuildGarden()
        {
            Box("Floating sandstone foundation", new Vector3(0,-1.3f,0), new Vector3(13.4f,2.1f,13.4f), "A4A080", board);
            Box("Lower rock terrace", new Vector3(.2f,-2.5f,.1f), new Vector3(10.8f,.9f,11.5f), "8C987A", board);
            Box("Garden soil", new Vector3(0,-.29f,0), new Vector3(13.8f,.4f,13.8f), "6F8555", board);
            Box("Soft grass terrace", new Vector3(0,-.04f,0), new Vector3(13.7f,.2f,13.7f), "83AA66", board);
            var random = new System.Random(76);
            for (int i = 0; i < 34; i++)
            {
                float angle = i * Mathf.PI * 2 / 34, x = Mathf.Cos(angle) * 5.4f, z = Mathf.Sin(angle) * 5.4f;
                if (Mathf.Abs(x) < 4 && Mathf.Abs(z) < 4) continue;
                Ball("Hanging ivy", new Vector3(x*1.23f,-.3f,z*1.23f), new Vector3(.65f,.7f,.6f), i%2==0?"5B8C58":"70A15E", board);
            }
            for (int i = 0; i < 20; i++)
            {
                float x = (float)(random.NextDouble()*8-4), z = (float)(random.NextDouble()*8-4);
                if (Mathf.Abs(x)<1.8f && Mathf.Abs(z)<1.9f) continue;
                Tree(new Vector3(x,.06f,z), .65f+(float)random.NextDouble()*.5f);
            }
            // A shallow pond, ruined arch, stepping stones, and small flower beds.
            Cylinder("Garden pond stone rim",new Vector3(-1.9f,.04f,2),new Vector3(3.8f,.12f,2.7f),"B9B598",board);
            Cylinder("Reflecting pool",new Vector3(-1.9f,.18f,2),new Vector3(3.5f,.02f,2.4f),"86BCBE",board);
            for(int i=0;i<5;i++) Box("Pond stepping stone",new Vector3(-3.1f+i*.65f,.26f,2),new Vector3(.48f,.16f,.55f),"D3CDB0",board);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Arch pedestal",new Vector3(side*1.05f,.16f,-.8f),new Vector3(.85f,.3f,.85f),"C6BE9B",board);
                Box("Ancient arch pillar",new Vector3(side*1.05f,1.12f,-.8f),new Vector3(.5f,1.7f,.55f),"E1D5B1",board);
                Box("Pillar capital",new Vector3(side*1.05f,2,.8f-1.6f),new Vector3(.78f,.25f,.78f),"D1CAA7",board);
            }
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI/8;
                var stone=Box("Arch voussoir",new Vector3(Mathf.Cos(a)*1.05f,2+Mathf.Sin(a)*.85f,-.8f),new Vector3(.44f,.46f,.58f),"E1D5B1",board);
                stone.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);
            }
            var crystal=Shape(PrimitiveType.Cube,"Heart of the garden",new Vector3(0,.65f,-.8f),new Vector3(.46f,.85f,.46f),"B6E4D0",board);
            crystal.localRotation=Quaternion.Euler(0,45,35);
            for(int i=0;i<38;i++)
            {
                float x=(float)(random.NextDouble()*9-4.5), z=(float)(random.NextDouble()*9-4.5);
                if(Mathf.Abs(x)<2.3f && Mathf.Abs(z)<2.5f) continue;
                Ball("Wildflowers",new Vector3(x,.16f,z),Vector3.one*.13f,i%3==0?"E4B5CB":i%3==1?"EFE0A2":"DFE9CF",board);
            }
        }

        void Tree(Vector3 p, float size)
        {
            Transform tree=new GameObject("Sculpted garden tree").transform;tree.SetParent(board,false);tree.localPosition=p;tree.localScale=Vector3.one*size;
            Cylinder("Trunk",new Vector3(0,.65f,0),new Vector3(.22f,.65f,.22f),"8B7950",tree);
            Ball("Dark canopy",new Vector3(0,1.55f,0),new Vector3(1.25f,1.3f,1.18f),"507F4F",tree);
            Ball("Sunlit canopy",new Vector3(-.28f,1.85f,.12f),new Vector3(.95f,.95f,.95f),"7FAA65",tree);
            Ball("Light leaves",new Vector3(.32f,1.62f,.3f),new Vector3(.85f,.85f,.85f),"99B877",tree);
        }

        void BuildPath()
        {
            for(int i=0;i<32;i++)
            {
                int row,col;
                if(i<=8){row=8;col=8-i;}else if(i<=16){row=16-i;col=0;}else if(i<=24){row=0;col=i-16;}else{row=i-24;col=8;}
                float x=(col-4)*1.65f,z=(row-4)*1.65f;
                float elevation= row==0 ? .8f : col==8 ? Mathf.Lerp(.8f,.15f,row/8f) : col==0 ? Mathf.Lerp(.8f,.15f,row/8f) : .15f;
                var root=new GameObject("Tile "+i+" · "+GameData.Tiles[i]).transform;root.SetParent(board,false);root.localPosition=new Vector3(x,elevation,z);
                Box("Raised stone path",new Vector3(0,.04f,0),new Vector3(1.58f,.34f,1.58f),"BAA987",root);
                Box("Paving surface",new Vector3(0,.235f,0),new Vector3(1.5f,.1f,1.5f),GameData.IsFate(i)?"ADA1BD":GameData.Prices[i]==0?"D9C593":"D7BCAC",root);
                for(int j=0;j<3;j++)Box("Inset paving brick",new Vector3(-.43f+j*.43f,.295f,j%2==0?.36f:-.35f),new Vector3(.31f,.015f,.22f),"F2DFCD",root);
                var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.2f,0);collider.size=new Vector3(1.58f,.45f,1.58f);
                root.gameObject.AddComponent<TileHit>().index=i;
                tilePoints[i]=new Vector3(x,elevation+.31f,z);
                if(GameData.Prices[i]>0)
                {
                    Vector3 inside=new Vector3(x==0?0:-Mathf.Sign(x)*1.2f,0,z==0?0:-Mathf.Sign(z)*1.2f);
                    // Property lots sit beside the road, never replace the path.
                    if(row!=0 && row!=8)inside.z=0;else inside.x=0;
                    Box("Property stone rim",inside+new Vector3(0,.11f,0),new Vector3(.92f,.2f,.92f),"C4C5A5",root);
                    Box("Property grass",inside+new Vector3(0,.23f,0),new Vector3(.78f,.06f,.78f),"628E54",root);
                    Sign(root,inside+new Vector3(0,.3f,0),GameData.Prices[i]+" G");
                }
                if(i%4==0)
                {
                    Box("Bridge support pillar",new Vector3(0,-2.45f,0),new Vector3(.65f,4.6f,.65f),"BCB696",root);
                    Box("Bridge capital",new Vector3(0,-.28f,0),new Vector3(1.15f,.35f,1.15f),"D6CBA7",root);
                }
                if(row==0)
                {
                    Box("Garden balustrade",new Vector3(0,.6f,-.78f),new Vector3(1.6f,.12f,.13f),"D5CAA8",root);
                    for(int j=-1;j<=1;j+=2)Box("Baluster",new Vector3(j*.57f,.4f,-.78f),new Vector3(.12f,.45f,.12f),"D5CAA8",root);
                }
            }
        }

        void Sign(Transform parent,Vector3 p,string label)
        {
            Cylinder("For-sale sign post",p+new Vector3(0,.3f,0),new Vector3(.045f,.35f,.045f),"B7A26E",parent);
            var sign=Box("Property price sign",p+new Vector3(0,.64f,0),new Vector3(.65f,.43f,.06f),"E7D7AD",parent);
            var text=new GameObject("Price label").AddComponent<TextMesh>();text.transform.SetParent(sign,false);
            text.transform.localPosition=new Vector3(0,0,-.55f);text.transform.localRotation=Quaternion.Euler(0,180,0);
            text.transform.localScale=new Vector3(1/.65f,1/.43f,1/.06f)*.035f;
            text.text=label;text.fontSize=42;text.characterSize=.15f;text.anchor=TextAnchor.MiddleCenter;text.color=Hex("6C694A");
        }

        void BuildClouds()
        {
            for(int i=0;i<9;i++)
            {
                var p=new Vector3(Mathf.Sin(i*2.1f)*18,-4.5f-i%3,Mathf.Cos(i*2.1f)*18);
                for(int j=0;j<3;j++)Ball("Distant floating cloud",p+new Vector3(j*1.9f,j%2*.6f,0),new Vector3(4,1.1f,2.7f),"E3EDEE",transform);
            }
        }

        public Transform MakeHero(int id,Transform parent)
        {
            string[] hair={"D5CFBA","D4A85E","E5D9A0","D1DCE3","4C5147","B5B2C6"};
            string[] coats={"718999","658CAC","8EAD87","5F718B","9DA879","9E91B2"};
            Transform hero=new GameObject(GameData.Heroes[id]+" · 3D prototype").transform;hero.SetParent(parent,false);
            Ball("Left boot",new Vector3(-.13f,.1f,.02f),new Vector3(.2f,.19f,.32f),"535C5C",hero);
            Ball("Right boot",new Vector3(.13f,.1f,.02f),new Vector3(.2f,.19f,.32f),"535C5C",hero);
            Shape(PrimitiveType.Capsule,"Coat",new Vector3(0,.45f,0),new Vector3(.46f,.33f,.36f),coats[id],hero);
            Box("Gold belt",new Vector3(0,.38f,.17f),new Vector3(.36f,.065f,.06f),"DECB97",hero);
            Ball("Hair",new Vector3(0,1.05f,0),new Vector3(.74f,.77f,.67f),hair[id],hero);
            Ball("Face",new Vector3(0,.98f,.12f),new Vector3(.59f,.55f,.52f),"F2D2B0",hero);
            for(int j=-1;j<=1;j+=2)
            {
                Ball("Eye",new Vector3(j*.12f,1.01f,.365f),new Vector3(.055f,.086f,.025f),"3D5056",hero);
                Ball("Eye highlight",new Vector3(j*.12f+.008f,1.03f,.381f),new Vector3(.018f,.023f,.012f),"FFFBE8",hero);
                Ball("Cheek",new Vector3(j*.2f,.91f,.335f),new Vector3(.09f,.046f,.025f),"DEAA9A",hero);
                Ball("Hand",new Vector3(j*.29f,.48f,.08f),new Vector3(.15f,.17f,.16f),"F2D2B0",hero);
                var fringe=Ball("Sculpted fringe",new Vector3(j*.18f,1.24f,.19f),new Vector3(.29f,.25f,.26f),hair[id],hero);
                fringe.localRotation=Quaternion.Euler(0,0,j*28);
            }
            if(id==1)
            {
                Cylinder("Crown band",new Vector3(0,1.4f,0),new Vector3(.47f,.065f,.47f),"D8BA70",hero);
                for(int j=-1;j<=1;j++)Box("Crown point",new Vector3(j*.14f,1.51f,.12f),new Vector3(.065f,.16f,.06f),"E7CA81",hero);
            }
            if(id==2)
            {
                for(int j=-1;j<=1;j+=2){var wing=Ball("Small wing",new Vector3(j*.32f,.64f,-.15f),new Vector3(.23f,.46f,.08f),"EEE7CF",hero);wing.localRotation=Quaternion.Euler(0,0,j*-25);}
            }
            if(id==3||id==1)
            {
                var sword=Box("Sword",new Vector3(.38f,.62f,.09f),new Vector3(.08f,.75f,.06f),"C5DADF",hero);sword.localRotation=Quaternion.Euler(0,0,-16);
                Box("Sword crossguard",new Vector3(.32f,.34f,.1f),new Vector3(.27f,.065f,.08f),"D0B581",hero);
            }
            return hero;
        }

        void MakeRing(Transform parent,string color) { Cylinder("Player-colored base",new Vector3(0,.025f,0),new Vector3(.75f,.025f,.75f),color,parent); }
        void SetLayer(Transform root,int layer) { root.gameObject.layer=layer;foreach(Transform child in root)SetLayer(child,layer); }
        void MakePortraits()
        {
            var camera=new GameObject("Portrait capture camera").AddComponent<Camera>();camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=.89f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.4f,.35f,0);camera.cullingMask=1<<8;
            camera.transform.position=new Vector3(101.8f,1.2f,103);camera.transform.LookAt(new Vector3(100,.81f,100));
            for(int i=0;i<6;i++)
            {
                Transform hero=MakeHero(i,transform);hero.position=new Vector3(100,0,100);SetLayer(hero,8);
                portraits[i]=new RenderTexture(256,256,16,RenderTextureFormat.ARGB32);portraits[i].Create();
                camera.targetTexture=portraits[i];camera.Render();hero.gameObject.SetActive(false);Destroy(hero.gameObject);
            }
            camera.targetTexture=null;Destroy(camera.gameObject);
        }

        void BuildArena()
        {
            arena=new GameObject("3 versus 3 battle stage").transform;arena.position=new Vector3(45,0,0);
            Cylinder("Battle terrace",new Vector3(0,-.35f,0),new Vector3(12,.35f,9),"B3B59A",arena);
            Cylinder("Arena grass",new Vector3(0,.04f,0),new Vector3(11.7f,.06f,8.7f),"8DA47B",arena);
            Cylinder("Arena stone",new Vector3(0,.12f,0),new Vector3(8,.07f,6),"C5C4A5",arena);
            for(int i=0;i<6;i++)
            {
                Transform hero=MakeHero(i<3?model.profile.team[i]:i,arena);hero.localPosition=new Vector3(i<3?-2:2,.2f,(i%3-1)*1.8f);hero.localScale=Vector3.one*1.3f;
                hero.localRotation=Quaternion.Euler(0,i<3?65:-65,0);MakeRing(hero,i<3?"8CBCA2":"CC9295");
            }
            for(int i=0;i<6;i++)Box("Arena column",new Vector3(Mathf.Cos(i*Mathf.PI/3)*5,.8f,Mathf.Sin(i*Mathf.PI/3)*3.5f),new Vector3(.45f,1.8f,.45f),"D1C5A2",arena);
            arena.gameObject.SetActive(false);
        }

        public void ShowBoard()
        {
            board.gameObject.SetActive(true);arena.gameObject.SetActive(false);Camera.cullingMask=~(1<<8);
            ApplyCamera();
        }
        public void ShowBattle()
        {
            if(arena){arena.gameObject.SetActive(false);Destroy(arena.gameObject);}
            BuildArena();
            interact=false;board.gameObject.SetActive(false);arena.gameObject.SetActive(true);
            Camera.orthographicSize=Screen.width>Screen.height?6.4f:8.3f;
            Camera.transform.position=new Vector3(56,11,15);Camera.transform.LookAt(new Vector3(45,.5f,0));
        }
        public void ResetView() { azimuth=38;zoom=12.4f;ApplyCamera(); }
        public void Zoom(float delta) { zoom=Mathf.Clamp(zoom+delta,6.5f,16);ApplyCamera(); }
        void ApplyCamera()
        {
            if(!Camera || !board.gameObject.activeSelf)return;
            landscape=Screen.width>Screen.height;
            Camera.orthographicSize=landscape?zoom*.64f:zoom;
            float a=azimuth*Mathf.Deg2Rad;
            Camera.transform.position=new Vector3(Mathf.Sin(a)*23,20,Mathf.Cos(a)*23);
            Camera.transform.LookAt(new Vector3(0,-.25f,0));
        }
        public void Refresh()
        {
            ReducedMotion=model.profile.reducedMotion;
            sunlight.shadows=model.profile.lowQuality?LightShadows.None:LightShadows.Soft;
            if(heroId!=model.profile.representative)
            {
                Vector3 p=pawns[0].position;Destroy(pawns[0].gameObject);pawns[0]=MakeHero(model.profile.representative,board);
                pawns[0].position=p;MakeRing(pawns[0],"EFD080");heroId=model.profile.representative;
            }
            foreach(var b in buildings)Destroy(b);buildings.Clear();
            for(int i=0;i<32;i++) if(model.levels[i]>0)
            {
                Transform building=new GameObject("Owned property level "+model.levels[i]).transform;building.SetParent(board,false);
                Vector3 p=tilePoints[i];p+=new Vector3(Mathf.Abs(p.x)>6?-Mathf.Sign(p.x)*1.2f:0,0,Mathf.Abs(p.z)>6?-Mathf.Sign(p.z)*1.2f:0);
                building.position=p;float h=.35f+model.levels[i]*.3f;
                Box("House walls",new Vector3(0,h/2,0),new Vector3(.65f,h,.6f),"E7D9B6",building);
                var roof=Box("House roof",new Vector3(0,h+.08f,0),new Vector3(.72f,.18f,.72f),"7A9C92",building);roof.localRotation=Quaternion.Euler(0,45,0);
                buildings.Add(building.gameObject);
            }
        }

        void Update()
        {
            if(!interact||moving)return;
            if(landscape!=(Screen.width>Screen.height))ApplyCamera();
            if(Input.touchCount==2)
            {
                var a=Input.GetTouch(0);var b=Input.GetTouch(1);
                float old=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);
                float now=Vector2.Distance(a.position,b.position);Zoom((old-now)*.018f);acceptedPress=false;return;
            }
            bool ui=EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if(Input.touchCount>0 && EventSystem.current)ui=EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            if(Input.GetMouseButtonDown(0)){press=Input.mousePosition;previousTouch=press;dragging=false;acceptedPress=!ui;}
            if(Input.GetMouseButton(0)&&acceptedPress)
            {
                Vector2 current=Input.mousePosition;
                if(Vector2.Distance(current,press)>8)dragging=true;
                if(dragging){azimuth+=(current.x-previousTouch.x)*.3f;ApplyCamera();}
                previousTouch=current;
            }
            if(Input.GetMouseButtonUp(0)&&acceptedPress)
            {
                if(!dragging)
                {
                    RaycastHit hit;
                    if(Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition),out hit,100))
                    { var tile=hit.collider.GetComponent<TileHit>();if(tile)onTile(tile.index); }
                }
                acceptedPress=false;
            }
            if(!ui&&Mathf.Abs(Input.mouseScrollDelta.y)>.01f)Zoom(-Input.mouseScrollDelta.y*.6f);
        }

        void OnDestroy()
        {
            foreach(var rt in portraits)if(rt){rt.Release();Destroy(rt);}
            foreach(var m in materials.Values)if(m)Destroy(m);
        }
    }
}
