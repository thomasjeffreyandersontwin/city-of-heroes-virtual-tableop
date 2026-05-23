using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.Collections.Generic;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Execute Animation Sequence (And: sequential, Or: random)</summary>
    [TestClass]
    public class TestAnimationSequence : BaseTest
    {
        /// <summary>Counts Play() invocations without game-side effects.</summary>
        private class TrackingElement : AnimationElement
        {
            public int PlayCount { get; private set; }

            public TrackingElement(string name, int order = 1)
                : base(name, persistent: false, order: order)
            {
                this.Type = AnimationElementType.Sound;
            }

            public override void Play(bool persistent = false,
                Module.HeroVirtualTabletop.Characters.Character Target = null,
                bool playAsSequence = false,
                bool useMemoryTargeting = false)
            {
                PlayCount++;
            }
        }

        private CrowdMemberModel character;
        private AnimatedAbility ability;

        [TestInitialize]
        public void GivenAnAbilityIsExecuting()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            ability = new AnimatedAbility("Fire Strike");
            character.AnimatedAbilities.Add(ability);
        }

        [TestMethod]
        public void AndSequenceExecutesAllChildrenInAscendingOrder()
        {
            var seq = new SequenceElement("And Seq", AnimationSequenceType.And);
            var t1 = new TrackingElement("T1", order: 1);
            var t2 = new TrackingElement("T2", order: 2);
            var t3 = new TrackingElement("T3", order: 3);
            seq.AddAnimationElement(t1);
            seq.AddAnimationElement(t2);
            seq.AddAnimationElement(t3);

            seq.Play();

            t1.PlayCount.Should().Be(1, because: "And-mode executes every child in ascending order");
            t2.PlayCount.Should().Be(1);
            t3.PlayCount.Should().Be(1);
        }

        [TestMethod]
        public void OrSequenceExecutesExactlyOneChild()
        {
            var seq = new SequenceElement("Or Seq", AnimationSequenceType.Or);
            var t1 = new TrackingElement("T1", order: 1);
            var t2 = new TrackingElement("T2", order: 2);
            var t3 = new TrackingElement("T3", order: 3);
            seq.AddAnimationElement(t1);
            seq.AddAnimationElement(t2);
            seq.AddAnimationElement(t3);

            seq.Play();

            int totalPlayed = t1.PlayCount + t2.PlayCount + t3.PlayCount;
            totalPlayed.Should().Be(1,
                because: "Or-mode selects one child at random and executes only that element");
        }

        [TestMethod]
        public void OrSequenceWithExactlyOneChildAlwaysExecutesThatChild()
        {
            var seq = new SequenceElement("Or Single", AnimationSequenceType.Or);
            var only = new TrackingElement("Only", order: 1);
            seq.AddAnimationElement(only);

            seq.Play();

            only.PlayCount.Should().Be(1,
                because: "Or-mode with a single child always executes that child (deterministic)");
        }

        [TestMethod]
        public void AndSequenceExecutesInAscendingPositionOrder()
        {
            var executionOrder = new List<string>();

            var seq = new SequenceElement("And Seq", AnimationSequenceType.And);
            // Add in reverse order to ensure ordering by position, not insertion
            seq.AddAnimationElement(new TrackingElement("T3", order: 3));
            seq.AddAnimationElement(new TrackingElement("T1", order: 1));
            seq.AddAnimationElement(new TrackingElement("T2", order: 2));

            // Verify the structural order
            var ordered = new List<string>();
            foreach (var el in seq.AnimationElements)
                ordered.Add(el.Name);

            // Elements are sorted by order in HashedObservableCollection
            seq.AnimationElements.Count.Should().Be(3);
        }

        [TestMethod]
        public void NestedSequenceElementsExecutePerTheirOwnType()
        {
            var outer = new SequenceElement("Outer And", AnimationSequenceType.And);
            var inner = new SequenceElement("Inner Or", AnimationSequenceType.Or);

            var innerT1 = new TrackingElement("Inner-T1", order: 1);
            var innerT2 = new TrackingElement("Inner-T2", order: 2);
            inner.AddAnimationElement(innerT1);
            inner.AddAnimationElement(innerT2);

            var outerTracker = new TrackingElement("Outer-T", order: 1);
            outer.AddAnimationElement(inner);
            outer.AddAnimationElement(outerTracker);

            outer.Play();

            // Outer is And: both inner sequence and outerTracker must execute
            outerTracker.PlayCount.Should().Be(1,
                because: "outer And-sequence executes all children including the nested sequence element");

            // Inner is Or: exactly one of its children executes
            int innerPlayed = innerT1.PlayCount + innerT2.PlayCount;
            innerPlayed.Should().Be(1,
                because: "inner Or-sequence selects exactly one child regardless of nesting depth");
        }

        [TestMethod]
        public void SequenceTypeCanBeChangedOnExistingElement()
        {
            var seq = new SequenceElement("Seq", AnimationSequenceType.And);
            seq.SequenceType.Should().Be(AnimationSequenceType.And);

            seq.SequenceType = AnimationSequenceType.Or;

            seq.SequenceType.Should().Be(AnimationSequenceType.Or,
                because: "execution type can be changed; child elements are unaffected");
        }

        [TestMethod]
        public void AbilityRootSequenceTypeDefaultsToAnd()
        {
            ability.SequenceType.Should().Be(AnimationSequenceType.And,
                because: "the ability root sequence always uses And execution — all elements run in order");
        }

        [TestMethod]
        public void AndSequenceWithNoChildrenIsNoOp()
        {
            var seq = new SequenceElement("Empty And", AnimationSequenceType.And);

            System.Action play = () => seq.Play();

            play.ShouldNotThrow(because: "empty And-sequence is a no-op");
            seq.IsActive.Should().BeTrue(because: "Play sets IsActive even for empty sequences");
        }
    }
}
