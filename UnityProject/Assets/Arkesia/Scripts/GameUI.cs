using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arkesia
{
    /// <summary>All eleven native uGUI screens; board and combat remain real 3D scenes underneath.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        ArkesiaApp app;
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform safe, root, page, scrollContent, dialog;
        Font font;
        Sprite rounded;
        RenderTexture worldPortrait;
        string search = "", heroFilter = "전체", synergySearch = "";
        int width, height;
        float y;
        readonly Color paper = BoardWorld.Hex("EEE8D6"), ink = BoardWorld.Hex("42554C"), muted = BoardWorld.Hex("8C826A"), gold = BoardWorld.Hex("B89B62"), green = BoardWorld.Hex("486B5A");
        GameModel M { get { return app.Model; } }
        bool Wide { get { return Screen.width > Screen.height; } }
        float W { get { return safe ? safe.rect.width : 390; } }
        float H { get { return safe ? safe.rect.height : 844; } }

        public void Initialize(ArkesiaApp owner)
        {
            app = owner;
            font = Resources.Load<Font>("NanumGothic");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = MakeRoundedSprite();
            canvas = new GameObject("Arkesia Native UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.matchWidthOrHeight = 0; scaler.referenceResolution = Wide ? new Vector2(1280, 720) : new Vector2(390, 844);
            safe = Node("Safe Area", canvas.transform); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            ApplySafeArea();
            worldPortrait = new RenderTexture(768, 512, 16, RenderTextureFormat.ARGB32); worldPortrait.Create();
            app.World.Camera.targetTexture = worldPortrait; app.World.Camera.Render(); app.World.Camera.targetTexture = null;
        }

        void ApplySafeArea()
        {
            width = Screen.width; height = Screen.height;
            scaler.referenceResolution = Wide ? new Vector2(1280, 720) : new Vector2(390, 844);
            Rect area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            safe.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            safe.offsetMin = safe.offsetMax = Vector2.zero; Canvas.ForceUpdateCanvases();
        }
        void Update()
        {
            if (canvas && (width != Screen.width || height != Screen.height)) { ApplySafeArea(); Draw(app.ScreenId); }
        }
        static RectTransform Node(string name, Transform parent)
        {
            RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
        }
        static void Fit(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        static void Position(RectTransform rt, float x, float top, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -top); rt.sizeDelta = new Vector2(w, h);
        }
        Image Panel(Transform parent, float x, float top, float w, float h, Color color, bool shadow = false)
        {
            RectTransform rt = Node("Panel", parent); Position(rt, x, top, w, h);
            Image image = rt.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            if (shadow) { Shadow s = rt.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(.12f,.2f,.15f,.22f); s.effectDistance = new Vector2(0,-3); }
            return image;
        }
        Text Label(Transform parent, string value, float x, float top, float w, float h, int size = 13, Color? color = null, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            RectTransform rt = Node(value.Length > 20 ? "Label" : value, parent); Position(rt, x, top, w, h);
            Text text = rt.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color ?? ink;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.alignment = anchor; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        Button Button(Transform parent, string value, float x, float top, float w, float h, Action action, bool enabled = true, bool secondary = false)
        {
            Image panel = Panel(parent, x, top, w, h, secondary ? BoardWorld.Hex("D9D1B8") : green, true); panel.raycastTarget = true;
            Button button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel; button.interactable = enabled;
            ColorBlock cb = button.colors; cb.highlightedColor = new Color(1.08f,1.08f,1.08f); cb.pressedColor = new Color(.8f,.85f,.8f); cb.disabledColor = new Color(.6f,.6f,.6f,.55f); button.colors = cb;
            if (action != null) button.onClick.AddListener(() => action());
            Label(panel.transform, value, 5, 0, w-10, h, 12, secondary ? ink : BoardWorld.Hex("F6EACB"), true, TextAnchor.MiddleCenter);
            return button;
        }
        void Picture(Transform parent, Texture texture, float x, float top, float w, float h)
        {
            RectTransform rt = Node("Unity-rendered 3D portrait", parent); Position(rt,x,top,w,h);
            RawImage image = rt.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
        }
        InputField InputBox(Transform parent,string placeholder,float x,float top,float w,Action<string> onChange=null)
        {
            Image p=Panel(parent,x,top,w,44,BoardWorld.Hex("FBF6E7"));p.raycastTarget=true;
            var field=p.gameObject.AddComponent<InputField>();field.targetGraphic=p;field.characterLimit=200;
            Text t=Label(p.transform,"",12,0,w-24,44,12,ink,false,TextAnchor.MiddleLeft);
            Text hint=Label(p.transform,placeholder,12,0,w-24,44,12,muted,false,TextAnchor.MiddleLeft);
            field.textComponent=t;field.placeholder=hint;
            if(onChange!=null)field.onValueChanged.AddListener(value=>onChange(value));
            return field;
        }
        public void Draw(GameScreen id)
        {
            if (root) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            dialog = null;
            root=Node("M-"+((int)id+1).ToString("000")+" "+GameData.Screens[(int)id],safe);Fit(root);
            if(id==GameScreen.Board){DrawBoard();return;}
            if(id==GameScreen.Battle){DrawBattle();return;}
            Panel(root,0,0,W,H,paper);
            Header(id);
            BuildScroll();
            switch(id)
            {
                case GameScreen.Home: Home();break;
                case GameScreen.Lobby: Lobby();break;
                case GameScreen.Heroes: Heroes();break;
                case GameScreen.Party: Party();break;
                case GameScreen.Synergy: Synergies();break;
                case GameScreen.Property: Property();break;
                case GameScreen.Fate: Fate();break;
                case GameScreen.Results: Results();break;
                case GameScreen.Settings: Settings();break;
            }
            scrollContent.sizeDelta=new Vector2(0,y+30);
            DrawNav(id);
        }
        void Header(GameScreen id)
        {
            Panel(root,0,0,W,70,BoardWorld.Hex("2D4940"));
            Label(root,"ARKESIA",20,14,180,27,23,BoardWorld.Hex("E4D4A8"),true);
            Label(root,"BLUE MARBLE · UNITY",21,44,220,15,8,BoardWorld.Hex("AABDAB"));
            Label(root,M.profile.gold.ToString("N0")+" G",W-130,20,105,30,15,gold,true,TextAnchor.MiddleRight);
            Label(root,"M-"+((int)id+1).ToString("000")+" · "+GameData.Screens[(int)id],20,81,230,20,10,muted);
            Button(root,"화면 목록",W-92,76,76,30,ScreenMenu,true,true);
        }
        void BuildScroll()
        {
            RectTransform viewport=Node("Scrollable screen",root);Position(viewport,0,116,W,Mathf.Max(140,H-190));
            Image mask=viewport.gameObject.AddComponent<Image>();mask.color=Color.white;viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            ScrollRect scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            scrollContent=Node("Screen content",viewport);scrollContent.anchorMin=new Vector2(0,1);scrollContent.anchorMax=new Vector2(1,1);scrollContent.pivot=new Vector2(.5f,1);
            scrollContent.anchoredPosition=Vector2.zero;scrollContent.sizeDelta=Vector2.zero;
            scroll.content=scrollContent;scroll.viewport=viewport;page=scrollContent;y=0;
        }
        float BodyW { get { return W-40; } }
        void Title(string kicker,string title,string description)
        {
            Label(page,kicker,20,y,BodyW,18,9,gold);y+=25;
            Label(page,title,20,y,BodyW,66,25,ink,true);y+=71;
            Label(page,description,20,y,BodyW,44,11,muted);y+=54;
        }
        void Note(string text) { Label(page,text,20,y,BodyW,40,10,muted);y+=48; }
        void FullButton(string text,Action action,bool enabled=true,bool secondary=false)
        { Button(page,text,20,y,BodyW,48,action,enabled,secondary);y+=60; }
        void DrawNav(GameScreen selected)
        {
            Panel(root,0,H-65,W,65,BoardWorld.Hex("2F493F"));
            int[] ids={0,2,3,4,10};string[] names={"홈","영웅","편성","시너지","설정"};
            for(int i=0;i<5;i++)
            {
                int screen=ids[i];Button b=Button(root,names[i],i*W/5+3,H-57,W/5-6,46,()=>app.Show((GameScreen)screen),true,true);
                b.GetComponent<Image>().color=(int)selected==screen?BoardWorld.Hex("D7C496"):BoardWorld.Hex("445C4E");
                b.GetComponentInChildren<Text>().color=(int)selected==screen?ink:BoardWorld.Hex("C3CBB4");
            }
        }
        void Home()
        {
            Title("CHRONICLES OF ARKESIA","잊혀진 길 위에,\n다시 모이는 영웅들","작은 주사위 하나가 써 내려가는 모험의 연대기.");
            Panel(page,20,y,BodyW,220,BoardWorld.Hex("A0AB8E"),true);Picture(page,worldPortrait,23,y+3,BodyW-6,214);y+=236;
            FullButton("모험의 문을 열다  →",()=>app.Show(GameScreen.Lobby));
            Button(page,"3D 보드 둘러보기",20,y,BodyW,44,()=>app.Show(GameScreen.Board),true,true);y+=58;
            Button(page,"나의 원정대",20,y,BodyW/2-5,80,()=>app.Show(GameScreen.Party),true,true);
            Button(page,"시너지 도감",25+BodyW/2,y,BodyW/2-5,80,()=>app.Show(GameScreen.Synergy),true,true);y+=100;
            Note("네이티브 Unity 프로토타입 · 로그인 / 온라인 대전 미연결");
        }
        void Lobby()
        {
            Title("GATHER YOUR PARTY","함께 떠날 준비","개발용 시연 방 · 실제 온라인 접속은 지원하지 않습니다.");
            Panel(page,20,y,BodyW,85,BoardWorld.Hex("DCD2B5"),true);
            Label(page,"초대 코드",35,y+12,150,20,10,muted);Label(page,"ARK824",35,y+36,190,32,25,ink,true);
            Button(page,"복사",W-92,y+23,56,40,()=>{GUIUtility.systemCopyBuffer="ARK824";Toast("시연 코드를 복사했습니다.");},true,true);y+=103;
            for(int i=0;i<4;i++)
            {
                int id=i;
                Panel(page,20,y,BodyW,76,BoardWorld.Hex(i<2?"F5EFDE":"E2DAC3"),true);
                if(i<2){Picture(page,app.World.portraits[i],25,y+2,65,68);Label(page,i==0?"나의 모험가 · 방장":"별빛모험가 · 시연",98,y+16,BodyW-95,23,13,ink,true);Label(page,i==1||M.ready?"준비 완료":"준비 중",98,y+43,150,20,10,i==1||M.ready?green:muted);}
                else Label(page,"친구를 초대하세요 · 빈 슬롯",20,y,BodyW,76,12,muted,false,TextAnchor.MiddleCenter);
                y+=88;
            }
            FullButton(M.ready?"준비 취소":"준비 완료",()=>{M.ready=!M.ready;Draw(GameScreen.Lobby);},true,true);
            FullButton("시연 게임 시작",()=>app.Show(GameScreen.Board),M.ready);
        }
        void HeroCard(Transform parent,int id,float x,float top,float w,float h,Action action,string tag=null)
        {
            Button b=Button(parent,"",x,top,w,h,action,true,true);
            Picture(b.transform,app.World.portraits[id],2,5,w-4,h-51);
            Label(b.transform,tag??GameData.Elements[id],5,7,w-10,18,8,muted,false,TextAnchor.MiddleCenter);
            Label(b.transform,GameData.Heroes[id],4,h-47,w-8,23,13,ink,true,TextAnchor.MiddleCenter);
            Label(b.transform,M.profile.owned.Contains(id)?"보유 중":"2,000 G",4,h-23,w-8,19,9,muted,false,TextAnchor.MiddleCenter);
        }
        void Heroes()
        {
            Title("HERO COLLECTION","당신의 영웅을 만나세요","입체 영웅 초상 · 3D 모델을 Unity 카메라로 렌더링합니다.");
            float searchTop=y;
            InputField input=InputBox(page,"영웅 이름 검색",20,y,BodyW,value=>{search=value;RebuildHeroList(searchTop+105);});input.SetTextWithoutNotify(search);y+=54;
            string[] filters={"전체","보유","미보유","성","암","화"};
            for(int i=0;i<filters.Length;i++){string f=filters[i];Button(page,f,20+i*(BodyW/6),y,BodyW/6-4,40,()=>{heroFilter=f;Draw(GameScreen.Heroes);},true,heroFilter!=f);}y+=51;
            heroListTop=y;RebuildHeroList(y);
            y=heroListTop+388;
        }
        RectTransform heroList;
        float heroListTop;
        void RebuildHeroList(float top)
        {
            if(heroList){heroList.gameObject.SetActive(false);Destroy(heroList.gameObject);}
            heroList=Node("Filtered heroes",page);Position(heroList,0,top,W,390);
            int slot=0;float cw=(BodyW-16)/3;
            for(int i=0;i<6;i++)
            {
                int id=i;bool owns=M.profile.owned.Contains(i);
                if(!GameData.Heroes[i].Contains(search))continue;
                if(heroFilter=="보유"&&!owns||heroFilter=="미보유"&&owns)continue;
                if(heroFilter!="전체"&&heroFilter!="보유"&&heroFilter!="미보유"&&!GameData.Elements[i].Contains(heroFilter))continue;
                HeroCard(heroList,id,20+(slot%3)*(cw+8),(slot/3)*190,cw,178,()=>HeroDetail(id));slot++;
            }
            if(slot==0)Label(heroList,"일치하는 영웅이 없습니다.",20,15,BodyW,40,12,muted);
        }
        void HeroDetail(int id)
        {
            RectTransform d=Dialog(GameData.Heroes[id],355);
            Picture(d,app.World.portraits[id],30,43,130,150);
            Label(d,GameData.Elements[id]+" 속성\n\n개발용 3D 모델\n보드 · 전투 공용",172,66,130,130,12,muted);
            Button(d,M.profile.owned.Contains(id)?"편성하러 가기":"2,000 계정 G · 해금",20,240,d.rect.width-40,48,()=>
            {
                if(M.profile.owned.Contains(id)){CloseDialog();app.Show(GameScreen.Party);}
                else if(M.BuyHero(id)){CloseDialog();Draw(GameScreen.Heroes);Toast("영웅 해금 완료 · 기기에 저장했습니다.");}
                else Toast("계정 골드가 부족합니다.");
            },M.profile.owned.Contains(id)||M.profile.gold>=2000);
        }
        void Party()
        {
            Title("YOUR EXPEDITION","나의 원정대","3명의 영웅을 편성하고, 보드 위에 설 대표를 선택하세요.");
            float cw=(BodyW-16)/3;
            for(int i=0;i<3;i++)
            {
                int slot=i,id=M.profile.team[i];float x=20+i*(cw+8);
                HeroCard(page,id,x,y,cw,180,()=>PickHero(slot),M.profile.representative==id?"★ 대표 영웅":"슬롯 "+(i+1));
                Button(page,M.profile.representative==id?"보드 위의 나":"대표 지정",x,y+190,cw,44,()=>{M.profile.representative=id;M.Save();app.Refresh();},true,true);
            }
            y+=255;
            Panel(page,20,y,BodyW,100,BoardWorld.Hex("DCD9BA"),true);Label(page,"◇ 모험의 동행 · 개발용 시너지",35,y+18,BodyW-30,25,13,ink,true);
            Label(page,"2인: 시작 보너스 +5% / 3인: +10%\n원작 카드 세트와 효과는 미검증입니다.",35,y+51,BodyW-30,42,10,muted);y+=122;
            FullButton("이 편성으로 준비하기",()=>{M.Save();app.Show(GameScreen.Lobby);});
            Note("서로 다른 영웅만 편성됩니다. 슬롯을 누르면 교체합니다.");
        }
        void PickHero(int slot)
        {
            RectTransform d=Dialog("슬롯 "+(slot+1)+" · 영웅 교체",470);float cw=(d.rect.width-56)/3;
            for(int i=0;i<M.profile.owned.Count;i++)
            {
                int id=M.profile.owned[i];HeroCard(d,id,20+(i%3)*(cw+8),55+(i/3)*180,cw,166,()=>{M.SwapHero(slot,id);CloseDialog();app.Refresh();});
            }
        }
        void Synergies()
        {
            Title("CARD SET SYNERGY","함께할 때 더 강하게","카드 세트의 구성원과 수집 현황을 확인하세요.");
            float start=y;
            InputField field=InputBox(page,"세트 이름 검색",20,y,BodyW,v=>{synergySearch=v;DrawSynergyCards(start+58);});field.SetTextWithoutNotify(synergySearch);y+=58;
            DrawSynergyCards(y);y+=500;
            Note("아래 구성·명칭·효과는 UI용 예시이며 원작 데이터가 아닙니다.");
        }
        RectTransform synergyList;
        void DrawSynergyCards(float top)
        {
            if(synergyList){synergyList.gameObject.SetActive(false);Destroy(synergyList.gameObject);}
            synergyList=Node("Filtered synergy cards",page);Position(synergyList,0,top,W,500);float dy=0;
            string[] names={"모험의 동행","전설의 발자취"};
            for(int set=0;set<2;set++)
            {
                if(!names[set].Contains(synergySearch))continue;
                Panel(synergyList,20,dy,BodyW,230,BoardWorld.Hex("F5EFDC"),true);
                Label(synergyList,names[set],35,dy+18,BodyW-30,23,16,ink,true);Label(synergyList,"미검증 · 개발 예시",35,dy+44,BodyW-30,18,9,muted);
                int collected=0;for(int i=0;i<3;i++){int hero=set*3+i;if(M.profile.owned.Contains(hero))collected++;Picture(synergyList,app.World.portraits[hero],35+i*90,dy+68,76,90);Label(synergyList,GameData.Heroes[hero],35+i*90,dy+151,76,20,10,muted,false,TextAnchor.MiddleCenter);}
                Label(synergyList,"보유 "+collected+" / 3     2인 기본 효과 → 3인 강화 효과",35,dy+183,BodyW-30,35,11,green);dy+=247;
            }
        }
        void Property()
        {
            if(GameData.Prices[M.selectedTile]==0)M.selectedTile=4;
            int id=M.selectedTile,level=M.levels[id];
            Title("YOUR NEXT TERRITORY",GameData.Tiles[id],"길 옆의 영지를 구매하고 건물을 올려보세요.");
            Picture(page,worldPortrait,20,y,BodyW,190);y+=204;
            Stat("소유 상태",level==0?"주인 없는 영지":"나의 영지 · "+level+"단계");
            Stat("보유 매치 골드",M.money.ToString("N0")+" G");Stat(level==0?"구매 가격":"건설 비용",M.PropertyCost(id)+" G");
            Stat("예시 통행료",(GameData.Prices[id]*level/5)+" → "+(GameData.Prices[id]*(level+1)/5)+" G");y+=16;
            FullButton(level>=3?"최대 건설 완료":level==0?"영지 구매":"건설하기",app.Purchase,level<3&&M.money>=M.PropertyCost(id));
            FullButton("보드로 돌아가기",()=>app.Show(GameScreen.Board),true,true);
            Note("구매와 건설은 로컬 시연이며 서버 정산은 미연결입니다.");
        }
        void Stat(string name,string value)
        {
            Panel(page,20,y,BodyW,45,BoardWorld.Hex("E2D8BD"));Label(page,name,32,y,BodyW/2,45,11,muted,false,TextAnchor.MiddleLeft);
            Label(page,value,20+BodyW/2,y,BodyW/2-12,45,12,ink,true,TextAnchor.MiddleRight);y+=52;
        }
        void Fate()
        {
            Title("A TWIST OF FATE","운명이 당신을 부릅니다","카드를 뒤집어 이번 모험의 선물을 확인하세요.");
            Image card=Panel(page,(W-210)/2,y,210,270,green,true);
            Label(card.transform,M.fateClaimed?"✦":"◇",0,42,210,100,66,BoardWorld.Hex("D9CA98"),false,TextAnchor.MiddleCenter);
            Label(card.transform,M.fateClaimed?"모코코의 선물":"FATE",10,162,190,50,23,BoardWorld.Hex("EFE2B9"),true,TextAnchor.MiddleCenter);
            Label(card.transform,M.fateClaimed?"매치 골드 +150 G":"운명의 카드를 뒤집으세요",10,222,190,30,11,BoardWorld.Hex("B9C5AD"),false,TextAnchor.MiddleCenter);y+=298;
            FullButton(M.fateClaimed?"보드로 돌아가기":"카드 뒤집기",()=>{if(M.fateClaimed)app.Show(GameScreen.Board);else{M.ClaimFate();Draw(GameScreen.Fate);}});
            Note("로컬 시연 결과 · 실제 서버 추첨과 카드 체인 판정 미연결");
        }
        void Results()
        {
            Title("THE END OF AN ADVENTURE","당신의 모험이 빛났습니다","예시 결과 화면 · 실제 경기 정산이 아닙니다.");
            Label(page,"♜",20,y,BodyW,90,65,gold,false,TextAnchor.MiddleCenter);y+=100;
            Stat("나의 순위","1위 · 모험의 개척자");Stat("예시 계정 골드 보상","+350 G · 실제 지급 없음");
            string[] names={"나의 모험가","별빛모험가","모코코친구","새벽의검"};int[] assets={4250,3180,2340,1260};
            for(int i=0;i<4;i++){Panel(page,20,y,BodyW,64,BoardWorld.Hex("F3ECD7"),true);Picture(page,app.World.portraits[i],28,y+2,55,60);Label(page,(i+1)+"위 · "+names[i],95,y+12,BodyW-90,22,12,ink,true);Label(page,"순자산 "+assets[i].ToString("N0")+" G",95,y+37,200,20,10,muted);y+=76;}
            FullButton("다시 모험하기",()=>{M.ready=false;app.Show(GameScreen.Lobby);});FullButton("홈으로",()=>app.Show(GameScreen.Home),true,true);
        }
        void Settings()
        {
            Title("MAKE IT YOURS","모험가의 설정","기기 설정과 로컬 채팅 시연을 확인하세요.");
            Toggle("효과음 음소거",M.profile.muted,()=>{M.profile.muted=!M.profile.muted;AudioListener.volume=M.profile.muted?0:1;M.Save();Draw(GameScreen.Settings);});
            Toggle("모션 축소",M.profile.reducedMotion,()=>{M.profile.reducedMotion=!M.profile.reducedMotion;M.Save();app.Refresh();});
            Toggle("저사양 모드 · 그림자 끄기",M.profile.lowQuality,()=>{M.profile.lowQuality=!M.profile.lowQuality;M.Save();app.Refresh();});
            Label(page,"원정대 채팅 · 로컬 시연",20,y,BodyW,30,15,ink,true);y+=40;
            string log=string.Join("\n",M.chat.GetRange(Mathf.Max(0,M.chat.Count-5),Mathf.Min(5,M.chat.Count)).ToArray());
            Panel(page,20,y,BodyW,145,BoardWorld.Hex("E0D7BD"));Label(page,log,32,y+12,BodyW-24,125,11,muted);y+=155;
            InputField input=InputBox(page,"메시지 입력",20,y,BodyW-64);Button(page,"전송",W-76,y,56,44,()=>{string text=input.text.Trim();if(text.Length==0)return;M.chat.Add("나 · "+text);Draw(GameScreen.Settings);});y+=60;
            FullButton("11개 실행 화면 PNG로 저장",app.CaptureAll,true,true);
            Note("음원·온라인 채팅·신고/차단은 미연결입니다.\n모션 축소와 그림자 설정은 실제 보드에 적용됩니다.");
        }
        void Toggle(string title,bool value,Action action)
        {
            Panel(page,20,y,BodyW,58,BoardWorld.Hex("F4EDD8"));Label(page,title,32,y,BodyW-90,58,12,ink,false,TextAnchor.MiddleLeft);
            Button(page,value?"ON":"OFF",W-88,y+8,54,42,action,true,!value);y+=70;
        }

        void DrawBoard()
        {
            Color dark=BoardWorld.Hex("2A493F");dark.a=.92f;
            Panel(root,12,10,Wide?280:W-24,62,dark,true);
            Label(root,"ARKESIA · 하늘정원",25,21,Wide?220:W-115,24,16,BoardWorld.Hex("EADBB3"),true);
            Label(root,"TURN "+M.turn.ToString("00")+" / 30   ·   로컬 시연",25,48,220,17,9,BoardWorld.Hex("ADC3B3"));
            Button(root,"☰",Wide?W-66:W-70,Wide?15:20,48,44,ScreenMenu,true,true);
            float px=Wide?W-210:12,py=Wide?100:84,pw=Wide?190:(W-32)/2;
            string[] names={"나의 모험가","별빛모험가","모코코친구","새벽의검"};int[] cash={M.money,1800,2240,1560};
            for(int i=0;i<4;i++)
            {
                float x=Wide?px:px+(i%2)*(pw+8),top=Wide?py+i*78:py+(i/2)*65;
                Color c=BoardWorld.Hex(i==0?"E7D6A4":i==1?"C4DAD8":i==2?"E1C3C2":"C8D2AF");c.a=.96f;
                Panel(root,x,top,pw,Wide?68:58,c,true);Picture(root,app.World.portraits[i==0?M.profile.representative:i],x+2,top+1,52,55);
                Label(root,names[i]+(i==0?" · 내 턴":""),x+57,top+10,pw-63,20,Wide?12:10,ink,true);
                Label(root,cash[i].ToString("N0")+" G",x+57,top+33,pw-63,18,Wide?12:10,muted);
            }
            float controlsTop=H-(Wide?112:154),controlsX=Wide?W*.22f:12,controlsW=Wide?W*.56f:W-24;
            Panel(root,controlsX,controlsTop,controlsW,Wide?97:139,dark,true);
            Label(root,GameData.Tiles[M.position]+" · 나의 턴",controlsX+15,controlsTop+11,controlsW-30,22,11,BoardWorld.Hex("D9DCBD"));
            Label(root,M.dieA+"    "+M.dieB,controlsX+18,controlsTop+39,110,48,28,BoardWorld.Hex("F5E6BE"),true,TextAnchor.MiddleCenter);
            Button(root,app.World.moving?"이동 중…":"주사위 굴리기",controlsX+133,controlsTop+38,controlsW-147,47,app.Roll,!app.World.moving,true);
            if(!Wide)Label(root,"드래그: 회전  ·  두 손가락: 확대 / 축소",controlsX+15,controlsTop+103,controlsW-30,19,9,BoardWorld.Hex("B3C5B5"),false,TextAnchor.MiddleCenter);
            float toolY=Wide?H-75:controlsTop-54;
            Button(root,"−",12,toolY,44,44,()=>app.World.Zoom(1.5f),true,true);
            Button(root,"+",63,toolY,44,44,()=>app.World.Zoom(-1.5f),true,true);
            Button(root,"전체",114,toolY,52,44,()=>app.World.ResetView(),true,true);
            Button(root,"로그",W-66,toolY,54,44,()=>app.Show(GameScreen.Settings),true,true);
        }
        void DrawBattle()
        {
            Color dark=BoardWorld.Hex("29483D");dark.a=.92f;
            Panel(root,12,12,W-24,78,dark,true);Label(root,"운명의 조우 · 3대3",26,24,W-110,28,19,BoardWorld.Hex("E8D9AF"),true);
            Label(root,"로컬 전투 시연 · 적 원정대 HP "+M.battleHp+"%",26,59,W-50,20,10,BoardWorld.Hex("B2C7B5"));
            Button(root,"☰",W-70,27,45,44,ScreenMenu,true,true);
            float top=H-290;Panel(root,12,top,W-24,273,paper,true);
            Label(root,GameData.Heroes[M.profile.team[0]]+"의 행동 · 에너지 "+M.energy+" / 3",27,top+15,W-54,26,14,ink,true);
            if(M.battleFinished)
            {
                Label(root,"시연 전투 종료",27,top+55,W-54,35,20,ink,true);
                Button(root,"예시 결과 보기",27,top+111,W-54,48,()=>app.Show(GameScreen.Results));
            }
            else if(!M.battleRolled)Button(root,"전투 주사위 굴리기",27,top+61,W-54,50,()=>{M.RollBattle();Draw(GameScreen.Battle);});
            else
            {
                Label(root,"주사위 "+M.battleDieA+" + "+M.battleDieB+" = "+(M.battleDieA+M.battleDieB),27,top+48,W-54,28,17,green,true);
                string[] skills={"기본 공격","빛의 일격","성역의 빛"};int[] min={2,5,8};float sw=(W-70)/3;
                for(int i=0;i<3;i++){int threshold=min[i];Button(root,skills[i]+"\n합계 "+min[i]+" 이상",27+i*(sw+8),top+89,sw,59,()=>SelectTarget(threshold),M.battleDieA+M.battleDieB>=threshold,true);}
            }
            Button(root,"보드로 돌아가기",27,top+176,W-54,44,()=>app.Show(GameScreen.Board),true,true);
            Label(root,"스킬·피해 수치와 종료 판정은 개발용 예시입니다.",27,top+237,W-54,25,9,muted);
        }
        void SelectTarget(int minimum)
        {
            RectTransform d=Dialog("공격 대상 선택",290);float cw=(d.rect.width-56)/3;
            for(int i=0;i<3;i++){int id=i;HeroCard(d,i+3,20+i*(cw+8),64,cw,165,()=>{M.target=id;M.Attack(minimum);CloseDialog();Draw(GameScreen.Battle);Toast(GameData.Heroes[id+3]+"에게 시연 피해 적용");});}
        }
        RectTransform Dialog(string title,float size)
        {
            CloseDialog();dialog=Node("Modal overlay",root);Fit(dialog);
            Image shade=dialog.gameObject.AddComponent<Image>();shade.color=new Color(.08f,.15f,.12f,.65f);shade.raycastTarget=true;
            float dw=Mathf.Min(370,W-24),x=(W-dw)/2,top=Mathf.Max(12,(H-size)/2);
            Image panel=Panel(dialog,x,top,dw,size,paper,true);RectTransform d=panel.rectTransform;
            Label(d,title,20,17,dw-70,28,17,ink,true);Button(d,"×",dw-56,7,44,44,CloseDialog,true,true);return d;
        }
        void CloseDialog() { if(dialog){dialog.gameObject.SetActive(false);Destroy(dialog.gameObject);dialog=null;} }
        public void ScreenMenu()
        {
            RectTransform d=Dialog("화면 둘러보기",Mathf.Min(H-30,690));
            RectTransform viewport=Node("Screen list viewport",d);Position(viewport,12,58,d.rect.width-24,d.rect.height-72);
            viewport.gameObject.AddComponent<Image>();viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            ScrollRect scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            RectTransform list=Node("Screen list",viewport);list.anchorMin=new Vector2(0,1);list.anchorMax=new Vector2(1,1);list.pivot=new Vector2(.5f,1);list.sizeDelta=new Vector2(0,630);
            scroll.content=list;scroll.viewport=viewport;
            for(int i=0;i<11;i++)
            {
                int id=i;Button(list,"M-"+(i+1).ToString("000")+"  "+GameData.Screens[i],6,6+i*51,d.rect.width-36,45,()=>{CloseDialog();app.Show((GameScreen)id);},true,i!=5);
            }
            Button(list,"11개 실제 화면 캡처 · F8",6,6+11*51,d.rect.width-36,45,()=>{CloseDialog();app.CaptureAll();},true,true);
        }
        public void Toast(string text) { StartCoroutine(ToastRoutine(text)); }
        IEnumerator ToastRoutine(string text)
        {
            Image box=Panel(safe,20,H-115,W-40,55,BoardWorld.Hex("E9DBB5"),true);
            Label(box.transform,text,12,3,W-64,49,11,ink,true,TextAnchor.MiddleCenter);
            yield return new WaitForSecondsRealtime(2.5f);if(box)Destroy(box.gameObject);
        }
        static Sprite MakeRoundedSprite()
        {
            const int size=48;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Bilinear;
            for(int yy=0;yy<size;yy++)for(int xx=0;xx<size;xx++)
            {
                float dx=Mathf.Max(0,Mathf.Abs(xx-23.5f)-18),dy=Mathf.Max(0,Mathf.Abs(yy-23.5f)-18);
                t.SetPixel(xx,yy,new Color(1,1,1,Mathf.Clamp01(6-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            t.Apply();return Sprite.Create(t,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
        }
        void OnDestroy()
        {
            if(worldPortrait){worldPortrait.Release();Destroy(worldPortrait);}
            if(rounded){Destroy(rounded.texture);Destroy(rounded);}
        }
    }
}
