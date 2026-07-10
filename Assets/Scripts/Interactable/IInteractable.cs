public interface IInteractable // Defines a contract for objects the player can interact with.
{ // Opens the IInteractable interface body.
    void Interact(); // Runs the object's interaction behavior when the player activates it.
    string GetInteractionPrompt(); // Returns the prompt text that should be shown to the player.
} // Closes the IInteractable interface body.
