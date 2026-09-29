# Ori_style_platformer (Unity)

Unity 6 (`Rigidbody2D.linearVelocity` 사용) 2D 플랫포머. 스크립트는 `Assets/3.Script/` 아래.

## 작업 규칙
- **인코딩 주의:** 아래 파일은 UTF-8이 아니라 CP949(추정)라 편집 도구로 저장하면 한글(`[Header]`, 주석)이 깨진다.
  IDE에서 UTF-8로 다시 저장한 뒤에만 수정할 것.
  - `Assets/3.Script/ETC/CameraCtrl.cs`
  - `Assets/3.Script/Enemy/`: `EnemyBase`, `FrogEnemy`, `EagleEnemy`, `JumperEnemy`, `MineEnemy`, `WallCrawlerEnemy`
- **직렬화 필드 이름을 바꾸지 말 것.** `GameScene.unity`에 값이 저장돼 있고 기본값과 많이 다르다
  (예: `moveSpeed: 10`, `bashForce: 40`). 이름을 바꾸면 값이 초기화된다.
- 새 스크립트를 추가하면 `.cs.meta`도 함께 커밋한다.

## Player 구조 (`Assets/3.Script/Player/`)
`PlayerCtrl`은 **partial class 하나**(컴포넌트 하나)다. 파일만 기능별로 나뉘어 있다.

| 파일 | 내용 |
|---|---|
| `PlayerCtrl.cs` | 직렬화 필드 전부(인스펙터 순서 고정용), 프로퍼티, `Update`/`FixedUpdate`, 이동, 지면 체크, 호버, 피격 진입, `Kill()` |
| `PlayerCtrl.State.cs` | `PlayerState` 열거형, `ChangeState`/`EnterState`/`ExitState`/`TickState`, `CanXxx` 규칙 |
| `PlayerCtrl.Jump.cs` | 점프, 점프 홀드, 슈퍼점프, 발판 통과 |
| `PlayerCtrl.Dash.cs` | 대시/바쉬/슈퍼점프 공용 트레일(타이머 방식) |
| `PlayerCtrl.Climb.cs` | 벽 감지, 벽타기 진입/퇴장, 벽점프 |
| `PlayerCtrl.Bash.cs` | 바쉬 조준 입력, 화살표, 발사 |
| `PlayerCtrl.Knockback.cs` | 넉백 |

### 상태 머신
상태: `Normal, Climbing, Dashing, WallJumping, BashAim, BashBurst, KnockedBack, Dead`.

- 이동 액션의 상태는 반드시 `ChangeState()`로만 바꾼다. 진입/퇴장 정리(애니메이터, 슬로모션, 무적, 트레일)는
  `EnterState`/`ExitState`에서 처리한다. bool 플래그(`isDashing` 등)를 새로 만들지 말 것.
- 어떤 동작이 어느 상태에서 허용되는지는 `CanMove`, `CanFlip`, `CanJump`, `CanDash`, `CanStartBash`,
  `CanBeKnockedBack` 프로퍼티 한 곳에서 정한다.
- 대시/벽점프 잠금/넉백/바쉬 발사는 코루틴이 아니라 `stateTimer`를 `TickState()`에서 진행한다.
  `TickState()`와 `TickTrails()`는 입력 잠금(`IsInputLocked`)과 무관하게 `Update` 맨 앞에서 돈다.
- `ExitState`는 `state`가 아직 **이전 상태**일 때 호출된다. 그 안에서 `state`를 검사하는 함수를 부르지 말 것.
- 상태와 별개로 남아 있는 플래그: `isHovering`, `isGrounded`, `isChargingSuperJump`.

### 새 액션 추가 순서
1. `PlayerState`에 상태 추가
2. `EnterState`/`ExitState`/`TickState`에 진입·퇴장·종료 처리
3. `CanXxx` 규칙에서 새 상태 허용 여부 결정 (기존 액션과의 충돌은 여기서 확인)
4. 입력 처리는 `PlayerCtrl.Xxx.cs`(partial)에 두고 `Update`에서 호출
5. 필요한 설정값 필드는 `PlayerCtrl.cs`에 추가 (인스펙터 순서 유지)

### 외부에서 쓰는 공개 API (변경 시 사용처 확인)
`IsClimbing`, `IsDashing`, `IsBashing`, `IsGrounded`, `IsDie`, `FacingDirection`, `State`, `Kill()`, `ApplyKnockback()`.
사용처: `CameraCtrl`, `SeinCtrl`, `PlayerHealth`, `EnemyBase`, `BulletBase`.

### 트레일
`SetDashTrails(true)`는 켜면서 예약된 끄기를 취소한다. 끌 때는 `ScheduleTrailsOff()`(마지막 호출 우선).
죽으면 `PlayerHealth`가 `PlayerCtrl.enabled = false`로 꺼서 `TickTrails`가 멈추므로, `Kill()`에서 트레일을 즉시 끈다.

## PlayerHealth
- 무적은 두 원인을 분리한다: `isInvincible`(바쉬 등 외부 요청, `SetInvincible`) / `isHitInvincible`(피격 직후 1초).
  판정은 `IsInvincible`. 서로의 종료가 상대 무적을 풀지 않게 해야 한다.
- 죽으면 `Kill()` → `PlayerCtrl` 비활성화 → 레이어 변경 → `RaiseDied`. 부활은 씬 재로드로만 이뤄진다.

## 알려진 사실 / 의도된 동작
- **벽에 매달리면 입력과 무관하게 항상 미끄러진다(`climbSlideSpeed`). 의도된 동작이다.** 오르내림을 추가하지 말 것.
- `maxbashtime`은 인스펙터 필드를 직접 깎는다(씬 값은 1). 필요하면 별도 타이머로 분리.
- `fastFall`(`v < 0`)은 대시/바쉬 중에도 적용된다(원본 동작 유지).

## 남은 개선 후보
- **접촉 피해 이중 경로:** `EnemyBase.OnTriggerStay2D`와 `PlayerCtrl.OnTriggerEnter2D`가 둘 다 `TakeDamage`를 호출한다.
  지금은 플레이어 무적(1초)이 둘째를 막는다. 무적 시간을 줄이거나 없앨 때 먼저 정리할 것.
- 적 스크립트(UTF-8 변환 후): `target` null 체크 누락(`Eagle`/`Jumper`/`Mine`), 좌우 반전 코드 중복
  (`Frog`는 스프라이트가 반대라 부호 반대), 문자열 코루틴 호출(`Hitanimation_co`), `EagleEnemy`의 `rb` null.
- 입력이 `Input.GetKeyDown` 직접 호출이라, 리바인딩/패드 지원 시 입력 계층이 필요하다.
- 기능이 더 늘면 `PlayerCtrl`을 기능별 MonoBehaviour로 분리 검토 (씬 값 이전 필요, 위험 큼).
- 자동 테스트 없음. 상태 규칙을 바꾸면 직접 플레이로 확인한다.
