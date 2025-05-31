public enum AppState
{
    Home,
    Tutorial,
    Questions,
    Voice
}

public static class AppStateManager {
    public static AppState CurrentState = AppState.Home;
}