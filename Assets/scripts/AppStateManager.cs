public enum AppState
{
    Home,
    Tutorial,
    Questions,
    Voice,
    ObjectDetector
}

public static class AppStateManager {
    public static AppState CurrentState = AppState.Home;
}