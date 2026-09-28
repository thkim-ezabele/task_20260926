namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Conventions;

/// <summary>
/// 도우미가 속성에 남기는 체크 제약 종류 표식입니다. 모델 확정 뒤 제거되어 마이그레이션 스냅숏에 남지 않습니다.
/// </summary>
internal enum EnumCheckConstraintKind : short
{
    None = 0,
    Code = 1,
    Flags = 2,
}
