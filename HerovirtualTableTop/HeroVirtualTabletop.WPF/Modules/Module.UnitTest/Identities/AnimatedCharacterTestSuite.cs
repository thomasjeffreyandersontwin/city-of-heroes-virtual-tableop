using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using System.Collections;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using System.IO;
using System.Linq;
using Moq;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace Module.UnitTest.Identities
{
    [TestClass]
    public class AnimatedCharacterTestSuite : BaseTest
    {
        private CharacterEditorViewModel characterEditorVM;
        private CrowdMemberModel character;

        [TestInitialize]
        public void TestInitialize()
        {
            ResetKeyBindGeneratorStatics();
            var keyHandlerMock = new Mock<IDesktopKeyEventHandler>();
            character = new CrowdMemberModel("Spyder");

            unityContainerMock.Setup(c => c.Resolve(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<Microsoft.Practices.Unity.ResolverOverride[]>()))
                .Returns((Type t, string name, Microsoft.Practices.Unity.ResolverOverride[] overrides) =>
                {
                    if (t == typeof(OptionGroupViewModel<AnimatedAbility>))
                        return new OptionGroupViewModel<AnimatedAbility>(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, keyHandlerMock.Object, eventAggregator, character.AnimatedAbilities, character);
                    if (t == typeof(OptionGroupViewModel<Identity>))
                        return new OptionGroupViewModel<Identity>(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, keyHandlerMock.Object, eventAggregator, character.AvailableIdentities as OptionGroup<Identity>, character);
                    if (t == typeof(OptionGroupViewModel<CharacterMovement>))
                        return new OptionGroupViewModel<CharacterMovement>(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, keyHandlerMock.Object, eventAggregator, character.Movements, character);
                    return null;
                });

            characterEditorVM = new CharacterEditorViewModel(busyServiceMock.Object, unityContainerMock.Object, keyHandlerMock.Object, eventAggregator);
            characterEditorVM.LoadCharacter(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(character, null));
        }

        [TestMethod]
        public void AddAbility_AddsToAnimatedCharcter()
        {
            (characterEditorVM.OptionGroups.FirstOrDefault(og => og.OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME) as OptionGroupViewModel<AnimatedAbility>).AddOptionCommand.Execute(null);
            Assert.AreEqual(character.AnimatedAbilities.Count, 1);
        }

        [TestMethod]
        public void DeleteAbility_RemovesAnimationFromCharacter()
        {
            (characterEditorVM.OptionGroups.FirstOrDefault(og => og.OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME) as OptionGroupViewModel<AnimatedAbility>).AddOptionCommand.Execute(null);
            (characterEditorVM.OptionGroups.FirstOrDefault(og => og.OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME) as OptionGroupViewModel<AnimatedAbility>).SelectedOption = character.AnimatedAbilities[0];
            (characterEditorVM.OptionGroups.FirstOrDefault(og => og.OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME) as OptionGroupViewModel<AnimatedAbility>).RemoveOptionCommand.Execute(null);
            Assert.AreEqual(character.AnimatedAbilities.Count, 0);
        }
    }
}
