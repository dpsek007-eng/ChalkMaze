namespace ChalkMaze
{
    /// 아이템은 4층부터 등장한다. 미로만 계속 걷는 단조로움을 깨고,
    /// "지금 이 순간의 판단"을 만들어 준다 — 지도 완성의 쾌감은 너무 늦게 오기 때문에.
    public enum ItemKind
    {
        Oil,      // 횃불 절반 회복. 즉시.
        Shovel,   // 벽 하나를 영구히 뚫는다. 지름길 개통.
        Plank,    // 구덩이 하나를 영구히 건넌다.
        Thread,   // 아리아드네의 실. 매듭을 묶고 언제든 그 자리로 귀환.
        Compass,  // 12걸음 동안 출구 방향을 가리킨다.
        Lantern   // 25걸음 동안 시야가 넓어진다.
    }

    public static class ItemInfo
    {
        public static string Name(ItemKind k) => k switch
        {
            ItemKind.Oil     => "기름병",
            ItemKind.Shovel  => "삽",
            ItemKind.Plank   => "판자",
            ItemKind.Thread  => "아리아드네의 실",
            ItemKind.Compass => "나침반",
            ItemKind.Lantern => "랜턴",
            _ => "?"
        };

        public static string Hint(ItemKind k) => k switch
        {
            ItemKind.Oil     => "횃불을 절반 되살린다",
            ItemKind.Shovel  => "벽을 뚫는다 · 영구",
            ItemKind.Plank   => "구덩이를 건넌다 · 영구",
            ItemKind.Thread  => "매듭을 묶고 언제든 귀환",
            ItemKind.Compass => "잠시 출구 방향이 보인다",
            ItemKind.Lantern => "잠시 더 멀리 보인다",
            _ => ""
        };

        public static readonly ItemKind[] All =
        {
            ItemKind.Oil, ItemKind.Shovel, ItemKind.Plank,
            ItemKind.Thread, ItemKind.Compass, ItemKind.Lantern
        };

        /// 가중치대로 하나 뽑는다. 바닥 드롭과 광고 보상이 같은 표를 쓴다.
        public static ItemKind Roll(System.Random rng)
        {
            int total = 0;
            foreach (var k in All) total += Weight(k);
            int roll = rng.Next(total), acc = 0;
            foreach (var k in All)
            {
                acc += Weight(k);
                if (roll < acc) return k;
            }
            return ItemKind.Oil;
        }

        /// 등장 가중치 — 기름이 흔하고 실이 귀하다.
        public static int Weight(ItemKind k) => k switch
        {
            ItemKind.Oil     => 30,
            ItemKind.Shovel  => 20,
            ItemKind.Plank   => 18,
            ItemKind.Compass => 14,
            ItemKind.Lantern => 18,
            ItemKind.Thread  => 8,
            _ => 0
        };
    }
}
