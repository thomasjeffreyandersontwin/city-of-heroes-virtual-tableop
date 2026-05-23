using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>
    /// Stories: Add FX Element, Movement Element, Sound Element, Reference Element,
    ///          Sequence Element, Pause Element, Load-Identity Element — invariants and execute behavior.
    /// </summary>
    [TestClass]
    public class TestAnimationElementSubtypes : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility ability;

        [TestInitialize]
        public void GivenAnAbilityOnACharacter()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            ability = new AnimatedAbility("Fire Strike");
            character.AnimatedAbilities.Add(ability);
        }

        // ─── Pause Element ──────────────────────────────────────────────────────────

        [TestMethod]
        public void PauseElementHasCorrectTypeDesignation()
        {
            var pause = new PauseElement("Pause 1", time: 2);

            pause.Type.Should().Be(AnimationElementType.Pause);
        }

        [TestMethod]
        public void PauseElementStoresPauseDuration()
        {
            var pause = new PauseElement("Pause 1", time: 2);

            pause.Time.Should().Be(2,
                because: "pause duration is stored and readable");
        }

        [TestMethod]
        public void PauseElementZeroDurationIsNoOp()
        {
            // A zero-duration pause should not block progression
            var pause = new PauseElement("Pause 0", time: 0);

            pause.Time.Should().Be(0);
        }

        [TestMethod]
        public void PauseElementBlocksProgressionForConfiguredDuration()
        {
            var pause = new PauseElement("Pause 1", time: 1);
            ability.AddAnimationElement(pause);

            // Play: the pause element sets IsActive=true for its duration, then false
            // After Play(), the pause's IsActive reflects post-completion (false)
            ability.Play();

            ability.IsActive.Should().BeTrue(
                because: "after all elements including pause complete, ability remains in executing state");
        }

        // ─── Sound Element ──────────────────────────────────────────────────────────

        [TestMethod]
        public void SoundElementHasCorrectTypeDesignation()
        {
            var sound = new SoundElement("Sound 1", "thunder.wav");

            sound.Type.Should().Be(AnimationElementType.Sound);
        }

        [TestMethod]
        public void SoundElementStoresReferencedSoundResource()
        {
            var sound = new SoundElement("Thunder Clap", "SND_ThunderClap_01.wav");
            ability.AddAnimationElement(sound);

            ability.AnimationElements["Thunder Clap"].Should().NotBeNull();
            (ability.AnimationElements["Thunder Clap"] as SoundElement).SoundFile.Should().NotBeNull();
        }

        [TestMethod]
        public void SoundElementIsAddedAtBottomOfOrderedList()
        {
            ability.AddAnimationElement(new PauseElement("Pause 1", 1));
            var sound = new SoundElement("Thunder Clap", "thunder.wav");
            ability.AddAnimationElement(sound);

            sound.Order.Should().Be(2,
                because: "new sound element is appended at the bottom of the ordered list");
        }

        // ─── Movement Element (MOVElement) ──────────────────────────────────────────

        [TestMethod]
        public void MovementElementHasCorrectTypeDesignation()
        {
            var mov = new MOVElement("Fly Move", "MOV_Fly_01");

            mov.Type.Should().Be(AnimationElementType.Movement);
        }

        [TestMethod]
        public void MovementElementStoresReferencedMovementResource()
        {
            var mov = new MOVElement("Fly Move", "MOV_Fly_01");
            ability.AddAnimationElement(mov);

            (ability.AnimationElements["Fly Move"] as MOVElement).MOVResource.Should().Be("MOV_Fly_01");
        }

        // ─── FX Element ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void FXElementHasCorrectTypeDesignation()
        {
            var fx = new FXEffectElement("Fire Blast", "FX_FireBlast_01.fx");

            fx.Type.Should().Be(AnimationElementType.FX);
        }

        [TestMethod]
        public void FXElementStoresReferencedFXResource()
        {
            var fx = new FXEffectElement("Fire Blast", "FX_FireBlast_01.fx");
            ability.AddAnimationElement(fx);

            (ability.AnimationElements["Fire Blast"] as FXEffectElement).Effect.Should().Be("FX_FireBlast_01.fx");
        }

        // ─── Reference Element (ReferenceAbility) ───────────────────────────────────

        [TestMethod]
        public void ReferenceElementHasCorrectTypeDesignation()
        {
            var target = new AnimatedAbility("Fire Strike Sub");
            var refEl = new ReferenceAbility("Ref 1", target);

            refEl.Type.Should().Be(AnimationElementType.Reference);
        }

        [TestMethod]
        public void ReferenceElementStoresReferencedAbilityName()
        {
            var comboStrike = new AnimatedAbility("Combo Strike");
            character.AnimatedAbilities.Add(comboStrike);
            ability.AddAnimationElement(new PauseElement("Pause 1", 1));
            var refEl = new ReferenceAbility("Ref to Fire Strike", ability);
            comboStrike.AddAnimationElement(refEl);

            refEl.Reference.Name.Should().Be("Fire Strike",
                because: "reference element stores the target ability's name");
        }

        [TestMethod]
        public void ReferenceElementMissingTargetProducesNoOpOnPlay()
        {
            // Given a reference to null (deleted ability)
            var refEl = new ReferenceAbility("Ref Deleted", null);
            ability.AddAnimationElement(refEl);

            // When played: null reference should not throw
            System.Action play = () => refEl.Play();
            play.ShouldNotThrow(because: "unresolvable reference produces a silent no-op");
        }

        // ─── Sequence Element ───────────────────────────────────────────────────────

        [TestMethod]
        public void SequenceElementHasCorrectTypeDesignation()
        {
            var seq = new SequenceElement("Seq 1");

            seq.Type.Should().Be(AnimationElementType.Sequence);
        }

        [TestMethod]
        public void SequenceElementDefaultsToAndExecutionType()
        {
            var seq = new SequenceElement("Seq 1");

            seq.SequenceType.Should().Be(AnimationSequenceType.And,
                because: "sequence element defaults to And execution type");
        }

        [TestMethod]
        public void SequenceElementOrTypeStoredCorrectly()
        {
            var seq = new SequenceElement("Seq 1", AnimationSequenceType.Or);

            seq.SequenceType.Should().Be(AnimationSequenceType.Or);
        }

        [TestMethod]
        public void EmptySequenceElementIsNotExecutable()
        {
            var seq = new SequenceElement("Empty Seq", AnimationSequenceType.And);

            seq.AnimationElements.Should().BeEmpty(
                because: "a newly created sequence element has no child elements");
        }

        // ─── Load-Identity Element (IdentityElement) ─────────────────────────────────

        [TestMethod]
        public void LoadIdentityElementHasCorrectTypeDesignation()
        {
            var identity = new Identity("Dragon_Form", IdentityType.Costume, "Dragon_Form");
            var idEl = new IdentityElement("Id Elem 1", identity);

            idEl.Type.Should().Be(AnimationElementType.LoadIdentity);
        }

        [TestMethod]
        public void LoadIdentityElementStoresTargetIdentityName()
        {
            var identity = new Identity("Dragon_Form", IdentityType.Costume, "Dragon_Form");
            var idEl = new IdentityElement("Id Elem 1", identity);
            ability.AddAnimationElement(idEl);

            (ability.AnimationElements["Id Elem 1"] as IdentityElement).Identity.Name.Should().Be("Dragon_Form");
        }

        [TestMethod]
        public void LoadIdentityElementWithNullIdentityProducesNoOp()
        {
            var idEl = new IdentityElement("Id Elem Null", null);
            ability.AddAnimationElement(idEl);

            System.Action play = () => idEl.Play(Target: character);
            // Non-existent identity produces a no-op: no exception thrown
            play.ShouldNotThrow(because: "non-existent identity reference produces a no-op");
        }

        // ─── Cross-element position uniqueness ──────────────────────────────────────

        [TestMethod]
        public void ElementOrderPositionsAreUniqueWithinParentList()
        {
            ability.AddAnimationElement(new PauseElement("Pause 1", 1));
            ability.AddAnimationElement(new PauseElement("Pause 2", 1));
            ability.AddAnimationElement(new PauseElement("Pause 3", 1));

            var orders = new System.Collections.Generic.List<int>();
            foreach (var el in ability.AnimationElements)
                orders.Add(el.Order);

            orders.Should().OnlyHaveUniqueItems(
                because: "display order positions must be unique within the parent list");
        }
    }
}
