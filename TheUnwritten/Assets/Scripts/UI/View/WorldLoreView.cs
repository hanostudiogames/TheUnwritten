using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

using Common;
using Cysharp.Threading.Tasks;
using TMPro;

using UI.Components;

namespace UI.View
{
    public class WorldLoreView : Common.View
    {
        public interface IListener
        {
            UniTask OnCompleteAsync();
        }
        
        [SerializeField] private RectTransform portraitBgRectTr = null;
        [SerializeField] private RectTransform landscapeBgRectTr = null;
        
        [Header("Start")] 
        [SerializeField] private RectTransform startRootRectTr = null;
        [SerializeField] private Typer titleTextTyper = null;
        [SerializeField] private Typer openingNarrativeTextTyper = null;
        [SerializeField] private Button startBtn = null;
        
        [Header("World Lore")]
        [SerializeField] private RectTransform worldLoreRootRectTr = null;
        [SerializeField] private Typer worldLoreTextTyper = null;
        [SerializeField] private Button worldLoreBtn = null;


        private IListener _listener = null;
        
        public override void Initialize()
        {
            base.Initialize();

            if (startBtn != null)
            {
                startBtn.onClick.RemoveAllListeners();
                startBtn.onClick?.AddListener(() =>
                {
                    if(startRootRectTr)
                        startRootRectTr.gameObject.SetActive(false);
                    
                    if(worldLoreRootRectTr)
                        worldLoreRootRectTr.gameObject.SetActive(true);
                    
                    worldLoreTextTyper?.Initialize(new Typer.Param(OnCompleteWorldLoreText)
                        .WithEndDelaySeconds(3f)
                        .WithTypingSpeed(0.05f));
                    
                    var local = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", "world_lore", LocalizationSettings.SelectedLocale);
                    worldLoreTextTyper?.TypeTextAsync(local);
                });

                if (worldLoreBtn != null)
                {
                    worldLoreBtn.onClick.RemoveAllListeners();
                    worldLoreBtn.onClick.AddListener(() =>
                    {
                        _listener?.OnCompleteAsync();
                    });
                    
                    worldLoreBtn.interactable = false;
                }
                
                startBtn.transform.parent.gameObject.SetActive(false);
            }
        }

        public override void Activate()
        {
            base.Activate();

            if(worldLoreRootRectTr)
                worldLoreRootRectTr.gameObject.SetActive(false);
            
            StartOpeningNarrativeTextAsync().Forget();
        }

        private async UniTask StartOpeningNarrativeTextAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            
            openingNarrativeTextTyper?.Initialize(new Typer.Param(OnCompleteOpeningNarrativeText)
                .WithEndDelaySeconds(2f)
                .WithTypingSpeed(0.1f));
            
            var local = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", "opening_narrative", LocalizationSettings.SelectedLocale);
            openingNarrativeTextTyper?.TypeTextAsync(local);
        }

        private void StartTitleText()
        {
            titleTextTyper?.Initialize(new Typer.Param(OnCompleteTitleText)
                .WithEndDelaySeconds(1f)
                .WithTypingSpeed(0.5f));
            
            var local = LocalizationSettings.StringDatabase.GetLocalizedString("Dialogue", "silence_of_gods", LocalizationSettings.SelectedLocale);
            titleTextTyper?.TypeTextAsync(local);
        }

        private void OnCompleteTitleText()
        {
            if (startBtn != null)
                startBtn.transform.parent.gameObject.SetActive(true);
        }

        private void OnCompleteOpeningNarrativeText()
        {
            StartTitleText();
        }

        private void OnCompleteWorldLoreText()
        {
            if (worldLoreBtn != null)
                worldLoreBtn.interactable = true;
        }

        public void AddListener(IListener listener)
        {
            _listener = listener;
        }
        
        private void OnRectTransformDimensionsChange()
        {
            bool isLandscape = Screen.width > Screen.height;
            
            if(landscapeBgRectTr)
                landscapeBgRectTr.gameObject.SetActive(isLandscape);
            
            if(portraitBgRectTr)
                portraitBgRectTr.gameObject.SetActive(!isLandscape);
        }
    }
}

