using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    [TestClass]
    public class AnimatedAbilityTestSuite : BaseTest
    {
        private AnimationElement FirstRootElementOfType(AnimationElementType type, int ordinal = 0)
        {
            return abilityEditorViewModel.CurrentAbility.AnimationElements
                .Where(a => a.Type == type)
                .OrderBy(a => a.Order)
                .Skip(ordinal)
                .First();
        }

        private SequenceElement FirstSequenceElement()
        {
            return abilityEditorViewModel.CurrentAbility.AnimationElements
                .OfType<SequenceElement>()
                .OrderBy(a => a.Order)
                .First();
        }

        private AbilityEditorViewModel abilityEditorViewModel;
        protected Mock<IResourceRepository> resourceRepositoryMock = new Mock<IResourceRepository>();
        protected Mock<IDesktopKeyEventHandler> keyEventHandlerMock = new Mock<IDesktopKeyEventHandler>();
        protected Mock<IHCSIntegrator> hcsIntegratorMock = new Mock<IHCSIntegrator>();
        private CrowdMemberModel character;
        [TestInitialize]
        public void TestInitialize()
        {
            ResetKeyBindGeneratorStatics();
            abilityEditorViewModel = new AbilityEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, resourceRepositoryMock.Object, keyEventHandlerMock.Object, hcsIntegratorMock.Object, eventAggregator);
            character = new CrowdMemberModel("Spyder");
            abilityEditorViewModel.CurrentAbility = new AnimatedAbility("Ability");
            abilityEditorViewModel.CurrentAbility.Owner = character;
            //this.abilityEditorViewModel.CurrentAbility = new AnimatedAbility("Ability");
            this.abilityEditorViewModel.Owner = character;

        }

        [TestMethod]
        public void AddAnimationElement_AddsAnimationToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.CurrentAbility.AnimationElements != null && this.abilityEditorViewModel.CurrentAbility.AnimationElements.Count == 1);
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperNumberSuffix()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
            // and so on...
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperType()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is MOVElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX); 
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is FXEffectElement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is SoundElement);
            // and so on...
        }
        [TestMethod]
        public void AddAnimationElement_AddsAnimationWithProperOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 1);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 2);
            this.abilityEditorViewModel.SelectedAnimationElement = FirstRootElementOfType(AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 2);
        }
        [TestMethod]
        public void RemoveAnimationElement_RemovesAnimationElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = FirstRootElementOfType(AnimationElementType.Movement);
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            var deletedElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements.Where(a => a.Type == AnimationElementType.Movement).FirstOrDefault();
            Assert.IsNull(deletedElement);
        }
        [TestMethod]
        public void RemoveAnimationElement_UpdatesOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = FirstRootElementOfType(AnimationElementType.Movement);
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            var updatedElement = this.abilityEditorViewModel.CurrentAbility.AnimationElements.Where(a => a.Type == AnimationElementType.FX).FirstOrDefault();
            Assert.AreEqual(updatedElement.Order, 1);
        }
        [TestMethod]
        public void AssignSequenceToAbility_AddsSequenceElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is SequenceElement);
        }
        [TestMethod]
        public void AssignSequenceToAbility_AddsSequenceElementWithDefaultSequenceAnd()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            Assert.IsTrue((this.abilityEditorViewModel.SelectedAnimationElement as SequenceElement).SequenceType == AnimationSequenceType.And);
        }
        [TestMethod]
        public void AddAnimationElementToParentSequence_NestsAnimationInParent()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Order == 1);
            var seq = FirstSequenceElement();
            Assert.IsTrue(seq.AnimationElements.Count == 1);
            Assert.IsTrue(seq.AnimationElements.OrderBy(a => a.Order).First().Name.StartsWith("Mov Element", StringComparison.Ordinal));
        }
        [TestMethod]
        public void AddAnimationElementToParentSequence_AddsChildrenInProperOrder()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            var seq = FirstSequenceElement();
            this.abilityEditorViewModel.SelectedAnimationParent = seq;
            this.abilityEditorViewModel.SelectedAnimationElement = seq.AnimationElements.First(a => a.Type == AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = seq.AnimationElements.OrderBy(a => a.Order).First(a => a.Type == AnimationElementType.Movement);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            var soundInsideSeq = seq.AnimationElements.First(a => a.Type == AnimationElementType.Sound);
            Assert.AreEqual(2, soundInsideSeq.Order);
        }
        [TestMethod]
        public void RemoveAnimationElementFromParentSequence_RemovesAnimationElementFromParent()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            var seq = FirstSequenceElement();
            this.abilityEditorViewModel.SelectedAnimationParent = seq;
            Assert.IsTrue(seq.AnimationElements.Count == 1);
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            Assert.IsTrue(seq.AnimationElements.Count == 0);
        }
        [TestMethod]
        public void RemoveAnimationElementFromParentSequence_UpdatesOrderInNestedElements()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sequence);
            this.abilityEditorViewModel.IsSequenceAbilitySelected = true;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            var seq = FirstSequenceElement();
            this.abilityEditorViewModel.SelectedAnimationParent = seq;
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            var soundInsideSeq = seq.AnimationElements.First(a => a.Type == AnimationElementType.Sound);
            Assert.AreEqual(3, soundInsideSeq.Order);
            var fxInsideSeq = seq.AnimationElements.First(a => a.Type == AnimationElementType.FX);
            this.abilityEditorViewModel.SelectedAnimationElement = fxInsideSeq;
            this.abilityEditorViewModel.RemoveAnimationCommand.Execute(null);
            soundInsideSeq = seq.AnimationElements.First(a => a.Type == AnimationElementType.Sound);
            Assert.AreEqual(2, soundInsideSeq.Order);
        }
        [TestMethod]
        public void AssignPauseElementToAbility_AddsPauseElement()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Pause);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement is PauseElement);
        }
        [TestMethod]
        public void AssignPauseElementToAbility_SetsDefaultPauseTimeToOne()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Pause);
            Assert.IsTrue((this.abilityEditorViewModel.SelectedAnimationElement as PauseElement).Time == 1);
        }
        [TestMethod]
        public void AssignMovToAbility_AddsMovElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationElementType.Movement);
        }
        [TestMethod]
        public void AssignMovToAbility_AddsMovWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Movement);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }
        [TestMethod]
        public void AssignFXToAbility_AddsFXElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationElementType.FX);
        }
        [TestMethod]
        public void AssignFXToAbility_AddsFXWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.FX);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }
        [TestMethod]
        public void AssignSoundToAbility_AddsSoundElementToAbility()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Type == AnimationElementType.Sound);
        }
        [TestMethod]
        public void AssignSoundToAbility_AddsSoundWithCorrectName()
        {
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("1"));
            this.abilityEditorViewModel.AddAnimationElementCommand.Execute(AnimationElementType.Sound);
            Assert.IsTrue(this.abilityEditorViewModel.SelectedAnimationElement.Name.EndsWith("2"));
        }

    }
}
