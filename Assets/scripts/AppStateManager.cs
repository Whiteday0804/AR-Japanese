public enum AppState
{
    Home,
    tutorial,
    questions,
    voice
}

public static class AppStateManager {
    public static AppState CurrentState = AppState.Home;
}