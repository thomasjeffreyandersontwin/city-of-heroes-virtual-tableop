using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Messages
{
    public static class Messages
    {
        #region General
        public const string GENERAL_UNHANDLED_EXCEPTION_MESSAGE = "An Exception was unhandled! Please see log for details!";
        public const string SELECT_GAME_DIRECTORY_MESSAGE = "Please select City of Heroes Game Directory";
        #endregion

        #region Character Explorer
        public const string DELETE_CONTAINING_CHARACTERS_FROM_CROWD_PROMPT_MESSAGE = "Do you want to delete every character specific to this crowd from the system as well?";
        public const string DELETE_CROWD_CAPTION = "Delete Crowd";
        public const string DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE = "This will remove this character from the system. Are you sure?";
        public const string DELETE_CHARACTER_CAPTION = "Delete Character";
        public const string DUPLICATE_NAME_MESSAGE = "Unable to rename as the provided name already exists.";
        public const string DUPLICATE_NAME_CAPTION = "Rename Crowd/Character";
        #endregion

        #region HCS
        public const string MOVEMENT_STOPPED_MESSAGE = "Movement ended";

        #endregion

    }
}
