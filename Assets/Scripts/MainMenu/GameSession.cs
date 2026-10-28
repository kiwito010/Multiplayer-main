using Fusion;

// Guarda los datos que se eligen en los menus, para que sobrevivan al
// cambiar de escena (MainMenu -> ChooseCharacter -> Gameplay).
// Es una clase "static": no hay que ponerla en ningun objeto, existe sola
// y cualquier script puede leerla (por ejemplo: GameSession.CharacterIndex).
public static class GameSession
{
    // Host (crear sala) o Client (unirse a una sala).
    public static GameMode Mode = GameMode.Host;

    // Codigo de la sala que el jugador escribio en el menu.
    public static string RoomCode = "";

    // Personaje elegido: 0 = Ingeniero, 1 = Hacker, 2 = Cientifico, 3 = Explorador.
    // Es la posicion en el array "Character Prefabs" del NetworkManagerFusion.
    public static int CharacterIndex = 0;
}
