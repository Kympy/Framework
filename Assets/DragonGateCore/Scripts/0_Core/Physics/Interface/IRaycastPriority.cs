namespace DragonGate
{
    /// <summary>
    /// 마우스 레이캐스트 후보 중 최적 대상을 선정할 때 사용하는 우선순위 인터페이스.
    /// 숫자가 높을수록 우선 선택된다. 동점이면 거리가 가까운 쪽이 선택된다.
    /// </summary>
    public interface IRaycastPriority
    {
        int RaycastPriority { get; }
    }
}