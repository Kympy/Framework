# VNFramework — Unity 비주얼 노벨 프레임워크

노드 기반 대화 에디터와 런타임 시스템을 포함한 미연시(Visual Novel) 개발 프레임워크입니다.

---

## 📁 폴더 구조

```
Assets/VNFramework/
├── Runtime/
│   ├── Data/
│   │   ├── VNTypes.cs          ← 열거형 + DialogueEvent + ChoiceData
│   │   ├── DialogueNode.cs     ← 노드 데이터 클래스
│   │   └── DialogueGraph.cs    ← ScriptableObject (그래프 에셋)
│   ├── Systems/
│   │   ├── DialogueRunner.cs   ← 대화 흐름 관리 (핵심 런타임)
│   │   └── EventExecutor.cs    ← 이벤트 실행 (배경·스프라이트·오디오·이펙트 등)
│   └── UI/
│       └── DialogueUI.cs       ← UI 표시 컴포넌트
└── Editor/
    ├── DialogueGraphEditorWindow.cs  ← 노드 그래프 에디터 창
    └── DialogueGraphInspector.cs     ← DialogueGraph 에셋 커스텀 인스펙터
```

---

## 🚀 시작하기

### 1. 패키지 임포트
이 폴더 전체를 `Assets/VNFramework/`에 복사합니다.

### 2. Dialogue Graph 생성
- **방법 A**: Project 창 우클릭 → `Create → VNFramework → Dialogue Graph`
- **방법 B**: 메뉴바 `VNFramework → Open Graph Editor` → 툴바의 `✨ New`

### 3. 에디터에서 대화 구성

| 조작 | 내용 |
|------|------|
| 툴바 `+ NPC / Player / Narration / End` | 노드 추가 |
| 캔버스 우클릭 | 노드 추가 컨텍스트 메뉴 |
| 노드 드래그 | 위치 이동 |
| **출력 포트(노란 점)** 드래그 → **입력 포트(파란 점)** | 노드 연결 |
| 노드 우클릭 | Start 지정 / 연결 해제 / 삭제 |
| `Delete` 키 | 선택 노드 삭제 |
| 캔버스 중간 버튼 드래그 | 뷰 이동 |
| 💾 Save | 에셋 저장 |

### 4. 씬에 런타임 배치

**GameObject 구성 예시**:
```
[Scene]
 ├── DialogueManager (GameObject)
 │    ├── DialogueRunner       ← DialogueRunner.cs
 │    └── EventExecutor        ← EventExecutor.cs
 └── Canvas
      └── DialogueUI (GameObject)
           └── DialogueUI      ← DialogueUI.cs
```

**DialogueRunner 설정 (Inspector)**:
- `Dialogue UI` → DialogueUI 컴포넌트 연결
- `Event Executor` → EventExecutor 컴포넌트 연결

**EventExecutor 설정 (Inspector)**:
- `Background Image` → 배경 Image UI 연결
- `Character Layers` → Left/Center/Right 캐릭터 Image 연결
- `Bgm Source` / `Sfx Source` → AudioSource 연결
- `Fade Panel` → CanvasGroup (검정 패널) 연결

**DialogueUI 설정 (Inspector)**:
- `Dialogue Panel` → 대화창 GameObject
- `Speaker Name Text` → TMP 텍스트
- `Dialogue Body Text` → TMP 텍스트  
- `Speaker Portrait Image` → 초상화 Image
- `Advance Button` → 다음 버튼
- `Choice Panel` → 선택지 패널
- `Choice Container` → 선택지 버튼 부모 Transform
- `Choice Button Prefab` → Button + TMP 텍스트 프리팹

### 5. 코드에서 실행

```csharp
using VNFramework;

public class GameManager : MonoBehaviour
{
    public DialogueGraph chapter1Graph;

    void Start()
    {
        var runner = DialogueRunner.Instance;

        // 대화 종료 이벤트
        runner.OnDialogueEnd += () => Debug.Log("대화 종료");

        // 챕터 전환 이벤트
        runner.OnChapterTransition += (chapterId) =>
            Debug.Log($"챕터 이동: {chapterId}");

        // 대화 시작
        runner.StartDialogue(chapter1Graph);
    }
}
```

---

## 🔧 노드 타입

| 타입 | 설명 |
|------|------|
| **Start** | 그래프 시작점. 내용 없이 다음 노드로 이동 |
| **NPC** | NPC 대사. 선택지 또는 Next로 분기 |
| **Player** | 플레이어 대사. 자동 진행 |
| **Narration** | 해설/독백. 화자 표시 없이 텍스트만 |
| **Chapter End** | 대화 종료 및 챕터 ID 전달 |

---

## ⚡ 이벤트 타입

노드 진입(Enter) / 퇴장(Exit) 시 실행할 수 있는 이벤트들:

| 이벤트 | 설명 |
|--------|------|
| `SetBackground` | 배경 이미지 변경 |
| `ShowCharacterSprite` | 캐릭터 스프라이트 표시 (Left/Center/Right) |
| `HideCharacterSprite` | 캐릭터 스프라이트 숨김 |
| `SetCharacterEmotion` | 캐릭터 표정 스프라이트 교체 |
| `PlayAnimation` | 씬 오브젝트의 Animator 트리거 발동 |
| `PlayEffect` | 파티클 이펙트 프리팹 생성 |
| `ShowUI` / `HideUI` | GameObject 이름으로 UI 표시/숨김 |
| `PlayBGM` | 배경음 재생 (loop) |
| `StopBGM` | 배경음 정지 |
| `PlaySFX` | 효과음 재생 |
| `FadeIn` / `FadeOut` | 페이드 인/아웃 (duration 설정) |
| `Wait` | 지정 시간(초) 대기 |

모든 이벤트에 `Wait For Completion` 옵션으로 이전 이벤트 완료 후 실행 가능.

---

## 📌 확장 포인트

- **조건부 선택지**: `ChoiceData`에 조건 필드 추가 후 `DialogueRunner`에서 체크
- **변수 시스템**: 별도 `GameVariables` ScriptableObject를 만들고 이벤트에서 참조
- **세이브/로드**: 현재 `nodeId`를 저장하고 `StartDialogue(graph, nodeId)`로 복원
- **자동 진행**: `DialogueUI`에 타이머 추가 후 `onAdvance` 자동 호출
- **번역**: `dialogueText`를 키로 사용하고 로컬라이제이션 테이블 연동
