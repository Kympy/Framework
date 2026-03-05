using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public interface ISaveData
    {
        string SaveTargetName { get; }
        /// <summary>새 게임 시작 시 초기 데이터를 생성합니다. 필요 없는 구현체는 오버라이드하지 않아도 됩니다.</summary>
        void CreateDefaultData() { }
        void Save(string saveKey, ES3Settings settings);
        void Load(string saveKey, ES3Settings settings);
    }

    [System.Serializable]
    public class SlotData
    {
        public int Index = -1;
        public long SaveTime = -1;     // UTC unix seconds (저장한 시각)
        public long LastPlayTime = -1; // UTC unix seconds (마지막으로 플레이한 시각)
    }

    // SaveRegistry: 어떤 슬롯이 존재하는지, 마지막으로 저장한 슬롯이 무엇인지 기록하는 목차.
    // 슬롯 파일들(Slot0.es3, Slot7.es3 ...)이 파편화되어 있어도 이 파일 하나로 전체 현황을 파악.
    [System.Serializable]
    public class SaveRegistry
    {
        public int NextSlotIndex = 0;
        public int LastSavedSlotIndex = -1;
        public List<int> SlotIndices = new List<int>(); // 최신순
    }

    // TSelf: 싱글턴 구체 타입
    // TSlot: 슬롯 메타데이터 타입 — 타이틀 미리보기 정보(직원 수, 골드 등)를 여기에 담는다
    public abstract class SaveSystem<TSelf, TSlot> : Singleton<TSelf>
        where TSelf : Singleton<TSelf>, new()
        where TSlot : SlotData, new()
    {
        /// <summary>현재 플레이 중인 슬롯. null이면 새 게임 상태.</summary>
        public TSlot CurrentSlot { get; private set; }
        public abstract int MaxSlotCount { get; }

        private readonly List<ISaveData> _saveTargets = new List<ISaveData>();
        protected IReadOnlyList<ISaveData> SaveTargets => _saveTargets;
        private SaveRegistry _cachedRegistry; // 매번 파일을 읽지 않도록 메모리에 캐시

        private const string SlotMetaKey = "SlotMeta";
        private const string RegistryKey = "Registry";

        // ES3Settings: 저장할 파일을 지정한다. 없으면 모든 데이터가 기본 파일 하나에 몰림.
        // 슬롯마다 별도 파일(Slot0.es3, Slot1.es3 ...)을 쓰기 때문에 필요하다.
        private ES3Settings RegistrySettings => new ES3Settings("SaveRegistry.es3");
        private ES3Settings GetSlotSettings(int slotIndex) => new ES3Settings($"Slot{slotIndex}.es3");

        // ─── 서브클래스 구현 ───────────────────────────────────────────────

        /// <summary>
        /// 저장 시점의 게임 상태로 슬롯 미리보기 데이터를 채워 반환.
        /// Index / SaveTime / LastPlayTime 은 SaveSystem이 자동으로 채운다.
        /// </summary>
        protected abstract TSlot BuildSlotData();

        // ─── Registry ─────────────────────────────────────────────────────

        private SaveRegistry LoadRegistry()
        {
            if (_cachedRegistry != null) return _cachedRegistry;
            _cachedRegistry = ES3.KeyExists(RegistryKey, RegistrySettings)
                ? ES3.Load<SaveRegistry>(RegistryKey, RegistrySettings)
                : new SaveRegistry();
            return _cachedRegistry;
        }

        private void SaveRegistry(SaveRegistry registry)
        {
            _cachedRegistry = registry;
            ES3.Save(RegistryKey, registry, RegistrySettings);
        }

        // ─── 슬롯 조회 (타이틀 UI용) ──────────────────────────────────────

        /// <summary>저장 데이터가 전혀 없으면 true</summary>
        public bool IsFirstPlay() => LoadRegistry().SlotIndices.Count == 0;

        /// <summary>가장 최근에 저장한 슬롯. 없으면 null. (이어하기 버튼용)</summary>
        public TSlot GetLastSavedSlot()
        {
            var registry = LoadRegistry();
            if (registry.LastSavedSlotIndex < 0) return null;
            return GetSlotData(registry.LastSavedSlotIndex);
        }

        /// <summary>저장된 모든 슬롯 목록 (최신순). 슬롯 선택 UI용.</summary>
        public List<TSlot> GetAllSlots()
        {
            var result = new List<TSlot>();
            foreach (var index in LoadRegistry().SlotIndices)
            {
                var data = GetSlotData(index);
                if (data != null) result.Add(data);
            }
            return result;
        }

        public TSlot GetSlotData(int slotIndex)
        {
            var settings = GetSlotSettings(slotIndex);
            if (!ES3.FileExists(settings)) return null;
            return ES3.Load<TSlot>(SlotMetaKey, settings);
        }

        // ─── 슬롯 상태 변경 ───────────────────────────────────────────────

        /// <summary>불러올 슬롯 지정. 씬 전환 전 Title에서 호출.</summary>
        public void SetCurrentSlot(int slotIndex)
        {
            CurrentSlot = GetSlotData(slotIndex);
            if (CurrentSlot == null) return;
            CurrentSlot.LastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ES3.Save(SlotMetaKey, CurrentSlot, GetSlotSettings(slotIndex));
        }

        /// <summary>새 게임 시작 시 호출. CurrentSlot을 null로 초기화.</summary>
        public void ClearCurrentSlot() => CurrentSlot = null;

        /// <summary>슬롯 파일 전체 삭제.</summary>
        public void DeleteSlot(int slotIndex)
        {
            ES3.DeleteFile(GetSlotSettings(slotIndex));
            var registry = LoadRegistry();
            registry.SlotIndices.Remove(slotIndex);
            if (registry.LastSavedSlotIndex == slotIndex)
                registry.LastSavedSlotIndex = registry.SlotIndices.Count > 0 ? registry.SlotIndices[0] : -1;
            SaveRegistry(registry);
        }

        // ─── ISaveData 등록 ───────────────────────────────────────────────

        public void AddSaveTarget(ISaveData target)
        {
            if (!_saveTargets.Contains(target))
                _saveTargets.Add(target);
        }
        public void RemoveSaveTarget(ISaveData target) => _saveTargets.Remove(target);

        /// <summary>등록된 모든 SaveTarget의 CreateDefaultData를 호출합니다. 새 게임 시작 시 사용.</summary>
        public void CreateAllDefaults()
        {
            foreach (var target in _saveTargets)
                target.CreateDefaultData();
        }

        // ─── 저장 / 불러오기 ──────────────────────────────────────────────

        /// <summary>
        /// BG3 스타일: 항상 새 슬롯 파일을 만들어 저장.
        /// MaxSlotCount 초과 시 가장 오래된 슬롯 파일 자동 삭제.
        /// </summary>
        public void SaveAsNewSlot()
        {
            DGDebug.Log("Save As New Slot...", Color.darkBlue);
            var registry = LoadRegistry();
            int newIndex = registry.NextSlotIndex++;

            CurrentSlot = BuildSlotData();
            CurrentSlot.Index = newIndex;
            CurrentSlot.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            CurrentSlot.LastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            registry.SlotIndices.Insert(0, newIndex);
            registry.LastSavedSlotIndex = newIndex;

            while (registry.SlotIndices.Count > MaxSlotCount)
            {
                int oldest = registry.SlotIndices[registry.SlotIndices.Count - 1];
                ES3.DeleteFile(GetSlotSettings(oldest));
                registry.SlotIndices.RemoveAt(registry.SlotIndices.Count - 1);
            }

            var settings = GetSlotSettings(newIndex);
            ES3.Save(SlotMetaKey, CurrentSlot, settings);
            foreach (var target in _saveTargets)
                target.Save(target.SaveTargetName, settings);

            SaveRegistry(registry);
        }

        /// <summary>현재 슬롯에 덮어쓰기 저장 (자동저장 등). CurrentSlot이 없으면 새 슬롯 생성.</summary>
        public void SaveAll()
        {
            DGDebug.Log("Save All.", Color.darkBlue);
            if (CurrentSlot == null) { SaveAsNewSlot(); return; }

            var updated = BuildSlotData();
            updated.Index = CurrentSlot.Index;
            updated.LastPlayTime = CurrentSlot.LastPlayTime;
            updated.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            CurrentSlot = updated;

            var settings = GetSlotSettings(CurrentSlot.Index);
            ES3.Save(SlotMetaKey, CurrentSlot, settings);

            var registry = LoadRegistry();
            registry.LastSavedSlotIndex = CurrentSlot.Index;
            registry.SlotIndices.Remove(CurrentSlot.Index);
            registry.SlotIndices.Insert(0, CurrentSlot.Index);
            SaveRegistry(registry);

            foreach (var target in _saveTargets)
                target.Save(target.SaveTargetName, settings);
            DGDebug.Log("Save All Done!", Color.darkBlue);
        }

        /// <summary>CurrentSlot 데이터로 불러오기. 인게임 씬 진입 시 호출.</summary>
        public void LoadAll()
        {
            if (CurrentSlot == null)
            {
                DGDebug.LogError("Current Slot is Null.");
                return;
            }
            var settings = GetSlotSettings(CurrentSlot.Index);
            foreach (var target in _saveTargets)
                target.Load(target.SaveTargetName, settings);
            DGDebug.Log("Load All Done!", Color.darkBlue);
        }
    }
}