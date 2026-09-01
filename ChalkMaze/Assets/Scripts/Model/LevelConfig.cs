using System;

namespace ChalkMaze
{
    /// 층마다 붙는 변형 규칙. 1000층을 만드는 것은 미로 개수가 아니라 이 조합이다.
    /// 크기만 키우면 9층과 500층이 똑같아진다.
    [Flags]
    public enum Mods
    {
        None        = 0,
        Blind       = 1 << 0,  // 복도가 안 보인다. 인접 칸만.
        FadingChalk = 1 << 1,  // 분필이 시간이 지나면 지워진다
        OneBonfire  = 1 << 2,  // 화톳불이 하나뿐
        NoChalk     = 1 << 3,  // 분필 없음. 순수 기억력
        Frugal      = 1 << 4,  // 횃불이 훨씬 짧다
        Reversed    = 1 << 5,  // 가장 깊은 곳에서 시작해 입구로 나간다
        KeyLock     = 1 << 6   // 열쇠를 찾아 잠긴 문으로 되돌아와야 한다
    }

    public static class ModInfo
    {
        public static string Name(Mods m) => m switch
        {
            Mods.Blind       => "암흑",
            Mods.FadingChalk => "지워지는 분필",
            Mods.OneBonfire  => "외딴 화톳불",
            Mods.NoChalk     => "분필 없음",
            Mods.Frugal      => "짧은 횃불",
            Mods.Reversed    => "역행",
            Mods.KeyLock     => "잠긴 문",
            _ => ""
        };

        public static string Desc(Mods m) => m switch
        {
            Mods.Blind       => "복도 끝이 보이지 않는다. 바로 옆 칸만.",
            Mods.FadingChalk => "분필 자국이 40걸음 뒤 사라진다.",
            Mods.OneBonfire  => "이 층의 화톳불은 하나뿐이다.",
            Mods.NoChalk     => "분필이 없다. 머리로만 기억해야 한다.",
            Mods.Frugal      => "횃불이 평소의 3분의 2도 못 간다.",
            Mods.Reversed    => "가장 깊은 곳에서 시작한다. 입구가 출구다.",
            Mods.KeyLock     => "출구가 잠겼다. 열쇠를 찾아 되돌아와야 한다.",
            _ => ""
        };

        public static readonly Mods[] Pool =
        {
            Mods.Blind, Mods.FadingChalk, Mods.OneBonfire,
            Mods.NoChalk, Mods.Frugal, Mods.Reversed, Mods.KeyLock
        };

        /// 규칙마다 처음 등장하는 층. 한꺼번에 쏟으면 배울 틈이 없다.
        public static int UnlockAt(Mods m) => m switch
        {
            Mods.KeyLock     => 12,
            Mods.Reversed    => 25,
            Mods.Frugal      => 40,
            Mods.OneBonfire  => 70,
            Mods.FadingChalk => 120,
            Mods.Blind       => 200,
            Mods.NoChalk     => 350,
            _ => 1
        };

        /// 정보를 빼앗는 규칙들. 이 중 둘 이상이 겹치면 실력이 개입할 여지가 사라진다.
        public static bool IsInfoLoss(Mods m) =>
            m == Mods.Blind || m == Mods.NoChalk || m == Mods.FadingChalk;
    }

    public struct LevelConfig
    {
        public int Size;
        public int Fuel;
        public int Chalk;
        public int Braid;
        public int Bonfires;
        public int Sight;        // 복도를 몇 칸까지 보는가
        public int Pickups;
        public int Pits;
        public bool ItemsOn;
        public bool PitsOn;
        public Mods Mods;
        public string Chapter;

        public const int ItemsFromLevel = 4;
        public const int PitsFromLevel  = 6;
        public const int ModsFromLevel  = 12;
        public const int FinalLevel     = 1000;

        public bool Has(Mods m) => (Mods & m) != 0;

        /// 층 번호만으로 결정된다. 같은 층은 누가 언제 해도 같은 조건이라
        /// "300층 해봤어?" 같은 대화가 성립한다.
        static uint Hash(int level, int salt)
        {
            unchecked
            {
                uint h = (uint)level * 2654435761u ^ (uint)(salt + 1) * 40503u;
                h ^= h >> 15; h *= 2246822519u;
                h ^= h >> 13; h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }

        static int Pick(int level, int salt, int lo, int hiInclusive)
            => lo + (int)(Hash(level, salt) % (uint)(hiInclusive - lo + 1));

        public static string ChapterOf(int level)
        {
            if (level <= 20)  return "채석장";
            if (level <= 99)  return "수몰층";
            if (level <= 299) return "뼈의 회랑";
            if (level <= 599) return "재의 심도";
            if (level <= 999) return "무광층";
            if (level == 1000) return "최심부";
            return "심연";
        }

        public static LevelConfig For(int level)
        {
            if (level < 1) level = 1;

            // ── 크기 : 초반은 성장, 이후는 변주 ──
            // 계속 키우면 화면에 안 들어가고 걷는 시간만 늘어난다.
            // 작은 미로 + 강한 변형이 큰 미로보다 어렵다.
            int size;
            if (level <= 7) size = 9 + 2 * (level - 1);
            else
            {
                int lo = level < 100 ? 13 : level < 400 ? 15 : 17;
                size = Pick(level, 1, lo, 21);
                if ((size & 1) == 0) size++;               // 홀수로 맞춰 격자를 안정시킨다
                if (size > 21) size = 21;
            }

            // ── 연료 : 깊어질수록 여유가 줄어든다 (0.42 → 0.26) ──
            double t = Math.Min(1.0, (level - 1) / 600.0);
            double ratio = 0.42 - 0.16 * t;

            // ── 분필 ──
            int chalk;
            if (level <= 8)        chalk = 7 - (level - 1) / 2;
            else if (level <= 60)  chalk = 3;
            else if (level <= 250) chalk = 2;
            else                   chalk = 1;

            // ── 화톳불 ──
            int fires = level < 40 ? 3 : level < 300 ? 2 : 1;

            // ── 변형 규칙 개수 ──
            int modCount =
                level < ModsFromLevel ? 0 :
                level < 100  ? 1 :
                level < 300  ? (int)(Hash(level, 9) % 2) + 1 :
                level < 600  ? 2 :
                level < 1000 ? (int)(Hash(level, 9) % 2) + 2 :
                               3;

            Mods mods = Mods.None;
            bool infoLossTaken = false;
            for (int i = 0; i < modCount; i++)
            {
                for (int tryN = 0; tryN < 24; tryN++)
                {
                    var m = ModInfo.Pool[Hash(level, 20 + i * 7 + tryN) % (uint)ModInfo.Pool.Length];
                    if ((mods & m) != 0) continue;
                    if (level < ModInfo.UnlockAt(m)) continue;
                    // 정보 박탈 규칙은 한 층에 하나까지
                    if (ModInfo.IsInfoLoss(m) && infoLossTaken) continue;
                    mods |= m;
                    if (ModInfo.IsInfoLoss(m)) infoLossTaken = true;
                    break;
                }
            }

            // 시야 : 예전엔 복도 끝까지 무제한이라 긴 복도에서 미로가 다 보였다.
            // 그러면 기억할 것이 남지 않는다. 깊어질수록 더 좁아진다.
            int sight = level <= 12 ? 4 : level <= 200 ? 3 : 2;

            if ((mods & Mods.NoChalk) != 0) chalk = 0;
            if ((mods & Mods.OneBonfire) != 0) fires = 1;
            if ((mods & Mods.Frugal) != 0) ratio *= 0.66;

            bool items = level >= ItemsFromLevel;
            bool pits  = level >= PitsFromLevel;

            return new LevelConfig
            {
                Size     = size,
                Fuel     = (int)Math.Round(size * size * ratio),
                Chalk    = chalk,
                Braid    = (int)(size * 0.6f),
                Bonfires = fires,
                Sight    = sight,
                ItemsOn  = items,
                PitsOn   = pits,
                // 상한을 둔다. 441칸 미로에 구덩이 499개를 놓을 수는 없다.
                Pickups  = items ? Math.Min(5, 2 + level / 60) : 0,
                Pits     = pits  ? Math.Min(6, 1 + level / 90) : 0,
                Mods     = mods,
                Chapter  = ChapterOf(level)
            };
        }

        /// 이 층에서 처음 등장하는 규칙 (안내 문구용)
        public static Mods NewIn(int level)
        {
            if (level <= 1) return Mods.None;
            var now = For(level).Mods;
            var before = Mods.None;
            for (int L = Math.Max(1, level - 1); L >= Math.Max(1, level - 30); L--)
                before |= For(L).Mods;
            return now & ~before;
        }
    }
}
