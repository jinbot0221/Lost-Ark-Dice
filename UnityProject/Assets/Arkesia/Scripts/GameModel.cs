using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkesia
{
    public enum GameScreen { Home, Lobby, Heroes, Party, Synergy, Board, Property, Fate, Battle, Results, Settings }

    [Serializable]
    public sealed class Profile
    {
        public int gold = 12400;
        public List<int> owned = new List<int> { 0, 1, 2, 3 };
        public int[] team = { 0, 1, 2 };
        public int representative;
        public bool muted, reducedMotion, lowQuality;
    }

    public static class GameData
    {
        public static readonly string[] Heroes = { "아만", "실리안", "니나브", "카단", "웨이", "샨디" };
        public static readonly string[] Elements = { "성 · 암", "성 · 화", "성 · 토", "암 · 뇌", "토 · 수", "수 · 성" };
        public static readonly string[] Screens = { "홈", "방 로비", "영웅 도감", "3인 편성", "시너지 도감", "메인 보드", "영지 구매·건설", "운명의 카드", "3대3 PvP", "결과 / 보상", "설정 / 채팅" };
        public static readonly string[] Tiles = {
            "모험의 시작", "레온하트", "로그힐", "모코코 보상", "루테란 성", "갈기파도 항구", "항구도시 창천", "카오스 게이트",
            "모코코 마을", "슈테른", "베른 성", "스퀘어홀", "위대한 성", "얼어붙은 바다", "로아룬", "운명의 카드",
            "칼라자 마을", "니아 마을", "별모래 해변", "대항해", "칸다리아 영지", "벨리온 유적지", "아리안오브", "운명의 카드",
            "플레체", "칼리나리", "엘네아드", "모코코 보상", "검은이빨 주둔지", "해상 낙원 페이토", "별빛 등대의 섬", "운명의 카드"
        };
        public static readonly int[] Prices = { 0,100,120,0,180,200,230,0,260,300,340,0,380,400,440,0,480,520,550,0,600,650,700,0,740,800,850,0,900,950,1000,0 };
        public static bool IsFate(int tile) { return tile == 15 || tile == 23 || tile == 31; }
    }

    /// <summary>Local presentation state. Not authoritative multiplayer game rules.</summary>
    public sealed class GameModel
    {
        const string ProfileKey = "Arkesia.Unity.Prototype.Profile.v1";
        public Profile profile;
        public int money = 2000, position, turn = 1, dieA = 3, dieB = 5, selectedTile = 4;
        public int battleHp = 100, energy, battleDieA, battleDieB, target = -1;
        public bool ready, fateClaimed, battleRolled, battleFinished;
        public readonly int[] levels = new int[32];
        public readonly List<string> chat = new List<string> { "시스템 · 로컬 시연 방입니다.", "별빛모험가 · 좋은 모험 해요!" };

        public GameModel()
        {
            try { profile = JsonUtility.FromJson<Profile>(PlayerPrefs.GetString(ProfileKey, "")); }
            catch (Exception) { profile = null; }
            if (!Valid(profile)) profile = new Profile();
        }

        static bool Valid(Profile p)
        {
            if (p == null || p.gold < 0 || p.owned == null || p.team == null || p.team.Length != 3) return false;
            var unique = new HashSet<int>(p.team);
            if (unique.Count != 3 || !unique.Contains(p.representative)) return false;
            foreach (int id in p.team) if (id < 0 || id >= 6 || !p.owned.Contains(id)) return false;
            foreach (int id in p.owned) if (id < 0 || id >= 6) return false;
            return true;
        }

        public void Save() { PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(profile)); PlayerPrefs.Save(); }
        public bool BuyHero(int id)
        {
            if (id < 0 || id >= 6 || profile.owned.Contains(id) || profile.gold < 2000) return false;
            profile.gold -= 2000; profile.owned.Add(id); Save(); return true;
        }
        public void SwapHero(int slot, int id)
        {
            if (slot < 0 || slot > 2 || !profile.owned.Contains(id)) return;
            int previous = profile.team[slot], other = Array.IndexOf(profile.team, id);
            if (other >= 0) profile.team[other] = previous;
            profile.team[slot] = id;
            if (Array.IndexOf(profile.team, profile.representative) < 0) profile.representative = id;
            Save();
        }
        public int PropertyCost(int tile) { return levels[tile] == 0 ? GameData.Prices[tile] : GameData.Prices[tile] / 2; }
        public bool BuyOrBuild(int tile)
        {
            if (tile < 0 || tile >= 32 || GameData.Prices[tile] == 0 || levels[tile] >= 3 || money < PropertyCost(tile)) return false;
            money -= PropertyCost(tile); levels[tile]++; return true;
        }
        public void NewFate() { fateClaimed = false; }
        public bool ClaimFate() { if (fateClaimed) return false; fateClaimed = true; money += 150; return true; }
        public void RollBattle()
        {
            if (battleRolled || battleFinished) return;
            battleDieA = UnityEngine.Random.Range(1, 7); battleDieB = UnityEngine.Random.Range(1, 7);
            energy = Mathf.Min(3, energy + 1 + (battleDieA == battleDieB ? 1 : 0)); battleRolled = true;
        }
        public bool Attack(int minimum)
        {
            if (!battleRolled || battleFinished || target < 0 || battleDieA + battleDieB < minimum) return false;
            battleHp = Mathf.Max(0, battleHp - (minimum == 2 ? 25 : minimum == 5 ? 35 : 45));
            battleRolled = false; battleFinished = battleHp == 0; return true;
        }
        public void ResetBattle() { battleHp = 100; energy = 0; battleRolled = battleFinished = false; target = -1; }
    }
}
