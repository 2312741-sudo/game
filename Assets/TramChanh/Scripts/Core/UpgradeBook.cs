using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    public enum UpgradeId { BiggerIceBin = 0, Pantry = 1, StringLights = 2, WholesaleDeal = 3, GoldenMenuBoard = 4, HelperWorker = 5 }

    public enum UpgradeStatus { Owned, NeedsStars, NeedsMoney, Available }

    public readonly struct UpgradeInfo
    {
        public UpgradeInfo(UpgradeId id, string name, string effect, int cost, int requiredStars)
        {
            Id = id;
            Name = name;
            Effect = effect;
            Cost = cost;
            RequiredStars = requiredStars;
        }

        public UpgradeId Id { get; }
        public string Name { get; }
        public string Effect { get; }
        public int Cost { get; }
        public int RequiredStars { get; }
    }

    /// <summary>Stall upgrades the player buys with money once the reputation is high enough. Pure C#, saved through <see cref="ISettingsStore"/>.</summary>
    public sealed class UpgradeBook
    {
        public const string KeyPrefix = "upgrade.";
        private static readonly UpgradeInfo[] Infos =
        {
            new UpgradeInfo(UpgradeId.BiggerIceBin, "Thùng đá lớn", "Chứa thêm 20 đá", 150000, 0),
            new UpgradeInfo(UpgradeId.Pantry, "Tủ kho", "Thạch, bột, giấy gói chứa thêm 20", 200000, 1),
            new UpgradeInfo(UpgradeId.StringLights, "Đèn dây", "Khách kiên nhẫn hơn 15%", 250000, 2),
            new UpgradeInfo(UpgradeId.WholesaleDeal, "Mối nhập sỉ", "Nguyên liệu rẻ hơn 10%", 300000, 3),
            new UpgradeInfo(UpgradeId.GoldenMenuBoard, "Bảng menu vàng", "Mọi đơn trả thêm 10%", 400000, 4),
            new UpgradeInfo(UpgradeId.HelperWorker, "Nhân viên phụ", "Tự nhận đơn ở bàn (lương 30.000đ mỗi đêm)", 350000, 3),
        };
        private readonly bool[] _owned = new bool[6];

        public static IReadOnlyList<UpgradeInfo> All => Infos;
        public static UpgradeInfo Info(UpgradeId id) => Infos[(int)id];
        public bool Owns(UpgradeId id) => _owned[(int)id];
        public void Grant(UpgradeId id) { _owned[(int)id] = true; }

        public int IceCapacityBonus => Owns(UpgradeId.BiggerIceBin) ? 20 : 0;
        public int OtherCapacityBonus => Owns(UpgradeId.Pantry) ? 20 : 0;
        public double PatienceMultiplier => Owns(UpgradeId.StringLights) ? 1.15 : 1d;
        public int SupplierDiscountPercent => Owns(UpgradeId.WholesaleDeal) ? 10 : 0;
        public int PriceBonusPercent => Owns(UpgradeId.GoldenMenuBoard) ? 10 : 0;
        public bool HasHelper => Owns(UpgradeId.HelperWorker);

        public UpgradeStatus StatusOf(UpgradeId id, GameProgress wallet, Reputation reputation)
        {
            if (Owns(id)) { return UpgradeStatus.Owned; }
            UpgradeInfo info = Info(id);
            if (reputation.Stars < info.RequiredStars) { return UpgradeStatus.NeedsStars; }
            return wallet.Money < info.Cost ? UpgradeStatus.NeedsMoney : UpgradeStatus.Available;
        }

        /// <summary>Buys an upgrade. Returns false and changes nothing when owned, too few stars or too little money.</summary>
        public bool TryBuy(UpgradeId id, GameProgress wallet, Reputation reputation)
        {
            if (wallet == null) { throw new ArgumentNullException(nameof(wallet)); }
            if (reputation == null) { throw new ArgumentNullException(nameof(reputation)); }
            if (StatusOf(id, wallet, reputation) != UpgradeStatus.Available) { return false; }
            if (!wallet.TrySpend(Info(id).Cost)) { return false; }
            Grant(id);
            return true;
        }

        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            foreach (UpgradeInfo info in Infos)
            {
                _owned[(int)info.Id] = store.TryGetFloat(KeyPrefix + info.Id, out float value) && value >= 0.5f;
            }
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            foreach (UpgradeInfo info in Infos) { store.SetFloat(KeyPrefix + info.Id, Owns(info.Id) ? 1f : 0f); }
        }
    }
}
