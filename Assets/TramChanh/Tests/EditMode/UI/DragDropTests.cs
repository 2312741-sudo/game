using NUnit.Framework;
using TramChanh.Core;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>Drag and drop in the supplier shop: the decision logic is plain C#, so it is tested without UI Toolkit.</summary>
    public sealed class DragDropTests
    {
        private SupplierContext _context;
        private DragRules _rules;

        [SetUp]
        public void SetUp()
        {
            _context = new SupplierContext
            {
                Stock = new StockBook(),
                Wallet = new GameProgress(),
                Supplier = new Supplier(),
                Upgrades = new UpgradeBook(),
                Reputation = new Reputation(),
            };
            _rules = new DragRules(_context);
        }

        private void GiveMoney(int amount) { _context.Wallet.RecordSale(new Sale(amount, QualityTier.Good, 80)); }

        private static DragPayload Ice => DragPayload.ForIngredient(Ingredient.Ice);

        [Test]
        public void Evaluate_OutsideTheBasket_IsOutsideZone()
        {
            GiveMoney(100000);
            Assert.That(_rules.Evaluate(Ice, DropZone.None), Is.EqualTo(DropVerdict.OutsideZone));
        }

        [Test]
        public void Evaluate_Ingredient_ValidNoMoneyOrStockFull()
        {
            Assert.That(_rules.Evaluate(Ice, DropZone.Basket), Is.EqualTo(DropVerdict.NoMoney));
            GiveMoney(100000);
            Assert.That(_rules.Evaluate(Ice, DropZone.Basket), Is.EqualTo(DropVerdict.Valid));
            _context.Stock.Add(Ingredient.Ice, 35);
            Assert.That(_rules.Evaluate(Ice, DropZone.Basket), Is.EqualTo(DropVerdict.StockFull));
        }

        [Test]
        public void Evaluate_AgreesWithTheBuyFunctionOfCore()
        {
            int[] moneyCases = { 0, 4900, 5000, 100000 };
            int[] stockCases = { 0, 30, 31, 40 };
            foreach (int money in moneyCases)
            {
                foreach (int fill in stockCases)
                {
                    var wallet = new GameProgress();
                    if (money > 0) { wallet.RecordSale(new Sale(money, QualityTier.Good, 80)); }
                    var stock = new StockBook();
                    stock.Add(Ingredient.Ice, fill);
                    var context = new SupplierContext { Stock = stock, Wallet = wallet, Supplier = new Supplier(), Upgrades = new UpgradeBook(), Reputation = new Reputation() };
                    bool expected = new DragRules(context).Evaluate(Ice, DropZone.Basket) == DropVerdict.Valid;
                    bool actual = new Supplier().TryBuyPack(stock, wallet, Ingredient.Ice);
                    Assert.That(expected, Is.EqualTo(actual), "money " + money + ", ice " + fill);
                }
            }
        }

        [Test]
        public void Drop_Ingredient_BuysThroughCore()
        {
            GiveMoney(20000);
            DropOutcome outcome = _rules.Drop(Ice, DropZone.Basket);
            Assert.That(outcome.Bought, Is.True);
            Assert.That(outcome.Verdict, Is.EqualTo(DropVerdict.Valid));
            Assert.That(outcome.Message, Is.EqualTo("Đã mua: Đá +10"));
            Assert.That(_context.Stock.Level(Ingredient.Ice), Is.EqualTo(Supplier.PackSize));
            Assert.That(_context.Wallet.Money, Is.EqualTo(15000));
        }

        [Test]
        public void Drop_Refused_ChangesNothing()
        {
            GiveMoney(1000);
            DropOutcome poor = _rules.Drop(Ice, DropZone.Basket);
            Assert.That(poor.Bought, Is.False);
            Assert.That(poor.Verdict, Is.EqualTo(DropVerdict.NoMoney));
            DropOutcome outside = _rules.Drop(Ice, DropZone.None);
            Assert.That(outside.Bought, Is.False);
            Assert.That(outside.Verdict, Is.EqualTo(DropVerdict.OutsideZone));
            Assert.That(_context.Stock.Level(Ingredient.Ice), Is.EqualTo(0));
            Assert.That(_context.Wallet.Money, Is.EqualTo(1000));
        }

        [Test]
        public void Drop_Upgrade_BuysAndThenIsAlreadyOwned()
        {
            GiveMoney(200000);
            DragPayload bin = DragPayload.ForUpgrade(UpgradeId.BiggerIceBin);
            Assert.That(_rules.Evaluate(bin, DropZone.Basket), Is.EqualTo(DropVerdict.Valid));
            Assert.That(_rules.Drop(bin, DropZone.Basket).Bought, Is.True);
            Assert.That(_context.Upgrades.Owns(UpgradeId.BiggerIceBin), Is.True);
            Assert.That(_context.Wallet.Money, Is.EqualTo(50000));
            Assert.That(_rules.Evaluate(bin, DropZone.Basket), Is.EqualTo(DropVerdict.AlreadyOwned));
            Assert.That(_rules.Drop(bin, DropZone.Basket).Bought, Is.False);
            Assert.That(_context.Wallet.Money, Is.EqualTo(50000));
        }

        [Test]
        public void Evaluate_Upgrade_NeedsStarsOrMoney()
        {
            DragPayload bin = DragPayload.ForUpgrade(UpgradeId.BiggerIceBin);
            Assert.That(_rules.Evaluate(bin, DropZone.Basket), Is.EqualTo(DropVerdict.NoMoney));
            GiveMoney(1000000);
            DragPayload lights = DragPayload.ForUpgrade(UpgradeId.StringLights);
            Assert.That(_context.Reputation.Stars, Is.LessThan(UpgradeBook.Info(UpgradeId.StringLights).RequiredStars));
            Assert.That(_rules.Evaluate(lights, DropZone.Basket), Is.EqualTo(DropVerdict.NeedsStars));
            Assert.That(_rules.Drop(lights, DropZone.Basket).Bought, Is.False);
            Assert.That(_context.Wallet.Money, Is.EqualTo(1000000));
        }

        [Test]
        public void Session_ClickWithoutMovingBuysNothing()
        {
            GiveMoney(20000);
            var session = new DragSession(_rules);
            session.Press(Ice, 100f, 100f);
            Assert.That(session.State, Is.EqualTo(DragState.Pressed));
            Assert.That(session.Move(102f, 101f), Is.False);
            DragRelease release = session.Release(DropZone.Basket);
            Assert.That(release.WasDrag, Is.False);
            Assert.That(release.Outcome.Bought, Is.False);
            Assert.That(session.State, Is.EqualTo(DragState.Idle));
            Assert.That(_context.Wallet.Money, Is.EqualTo(20000));
        }

        [Test]
        public void Session_DragOntoBasket_Buys()
        {
            GiveMoney(20000);
            var session = new DragSession(_rules);
            session.Press(Ice, 100f, 100f);
            Assert.That(session.Move(100f + DragSession.StartDistance + 1f, 100f), Is.True);
            Assert.That(session.State, Is.EqualTo(DragState.Dragging));
            Assert.That(session.Hover(DropZone.Basket), Is.EqualTo(DropVerdict.Valid));
            DragRelease release = session.Release(DropZone.Basket);
            Assert.That(release.WasDrag, Is.True);
            Assert.That(release.Outcome.Bought, Is.True);
            Assert.That(_context.Stock.Level(Ingredient.Ice), Is.EqualTo(Supplier.PackSize));
            Assert.That(session.State, Is.EqualTo(DragState.Idle));
        }

        [Test]
        public void Session_DropOutsideOrPoor_CardGoesBackAndNothingIsBought()
        {
            GiveMoney(1000);
            var session = new DragSession(_rules);
            session.Press(Ice, 0f, 0f);
            session.Move(50f, 0f);
            Assert.That(session.Hover(DropZone.Basket), Is.EqualTo(DropVerdict.NoMoney));
            DragRelease poor = session.Release(DropZone.Basket);
            Assert.That(poor.WasDrag, Is.True);
            Assert.That(poor.Outcome.Bought, Is.False);

            GiveMoney(20000);
            session.Press(Ice, 0f, 0f);
            session.Move(50f, 0f);
            Assert.That(session.Hover(DropZone.None), Is.EqualTo(DropVerdict.OutsideZone));
            DragRelease outside = session.Release(DropZone.None);
            Assert.That(outside.Outcome.Verdict, Is.EqualTo(DropVerdict.OutsideZone));
            Assert.That(outside.Outcome.Bought, Is.False);
            Assert.That(_context.Wallet.Money, Is.EqualTo(21000));
            Assert.That(_context.Stock.Level(Ingredient.Ice), Is.EqualTo(0));
        }

        [Test]
        public void Session_CancelBuysNothing_AndHoverBeforeDragIsIgnored()
        {
            GiveMoney(20000);
            var session = new DragSession(_rules);
            Assert.That(session.Move(10f, 10f), Is.False);
            session.Press(Ice, 0f, 0f);
            Assert.That(session.Hover(DropZone.Basket), Is.EqualTo(DropVerdict.OutsideZone));
            session.Move(40f, 0f);
            session.Cancel();
            Assert.That(session.State, Is.EqualTo(DragState.Idle));
            Assert.That(_context.Wallet.Money, Is.EqualTo(20000));
        }

        [Test]
        public void MessageFor_HasAVietnameseLineForEveryVerdict()
        {
            foreach (DropVerdict verdict in System.Enum.GetValues(typeof(DropVerdict)))
            {
                Assert.That(DragRules.MessageFor(verdict), Is.Not.Null.And.Not.Empty, verdict.ToString());
            }
            Assert.That(DragRules.GhostText(DragPayload.ForUpgrade(UpgradeId.Pantry)), Is.EqualTo("Tủ kho"));
        }
    }
}
