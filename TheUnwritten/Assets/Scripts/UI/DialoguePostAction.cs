using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Common;
using DG.Tweening;
using TMPro;
using UI.Effects;

namespace UI
{
    public class DialoguePostAction
    {
        private const float MonsterAbsorbGrowStrength = 0.22f;
        private const float MonsterAbsorbGrowDuration = 0.18f;
        private const float MonsterAbsorbShakeSettleDuration = 0.18f;
        private const float MonsterAbsorbShakeStrength = 1.6f;

        public class Param
        {
            public List<TextMeshProUGUI> TMPs { get; private set; } = new();
            
            public int ActIndex { get; private set; }
            public int SceneIndex { get; private set; }
            
            public List<DialogueAction> DialogueActions { get; private set; } = new();
            public TextMeshProUGUI MonsterTMP { get; private set; } = null;

            public Param(List<TextMeshProUGUI> tmps, int actIndex, int sceneIndex)
            {
                TMPs = tmps;
                ActIndex = actIndex;
                SceneIndex = sceneIndex;
            }

            public Param WithDialogueActions(List<DialogueAction> dialogueActions)
            {
                DialogueActions = dialogueActions;
                return this;
            }

            public Param WithMonsterTMP(TextMeshProUGUI monsterTMP)
            {
                MonsterTMP = monsterTMP;
                return this;
            }
        }
        
        private Param _param = null;
        
        public DialoguePostAction SetParam(Param param)
        {
            _param = param;
            
            return this;
        }

        private List<TextMeshProUGUI> GetTMPs(int count)
        {
            var tmps = _param?.TMPs;
            if (tmps == null)
                return null;

            if (count <= 0)
                return tmps;
            
            var resTmps = new List<TextMeshProUGUI>();
            
            for (int i = 0; i < tmps.Count; ++i)
            {
                var tmp = tmps[i];
                if (tmp == null)
                    continue;
                
                if(count > resTmps.Count)
                    resTmps.Add(tmp);
            }

            return resTmps;
        }
        
        public async UniTask ExecuteAsync()
        {
            var dialogueActions = _param?.DialogueActions;
            if (dialogueActions == null)
                return;

            for (int i = 0; i < dialogueActions.Count; ++i)
            {
                var action = dialogueActions[i];
                if(action == null)
                    continue;
                
                var tmps = GetTMPs(action.TmpCount);
                if (tmps == null)
                    continue;
                
                // await UniTask.Delay(TimeSpan.FromSeconds(action.StartDelay));
                float blockingDuration = 0f;
                bool delayHandled = false;
                
                switch (action.DialogueActionType)
                {
                    case DialogueActionType.Shear:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoShear(1f, action.Duration)?
                                .SetDelay(action.StartDelay);
                        }

                        break;
                    }

                    case DialogueActionType.Fold:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoFold(action.TargetValue, action.Duration)?
                                .SetDelay(action.StartDelay);
                        }
       
                        break;
                    }

                    case DialogueActionType.RandomShake:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp.DORandomShake(7f, action.Duration, 0.05f);
                        }

                        break;
                    }

                    case DialogueActionType.Melt:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoMelt(1f, action.Duration)?
                                .SetDelay(action.StartDelay);
                        }

                        break;
                    }

                    case DialogueActionType.RandomMelt:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DORandomMelt(action.TargetValue, action.Duration, 0.05f)?
                                .SetDelay(action.StartDelay);
                        }

                        break;
                    }
                    
                    case DialogueActionType.Shake:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp.DoShake(7f, action.Duration);
                        }
                        
                        break;
                    }
                    
                    case DialogueActionType.RandomCollapse:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp.DORandomCollapse(action.Duration, 0.01f);
                        }

                        break;
                    }

                    case DialogueActionType.Pulse:
                    {
                        // TargetValue >0: 글자 확대 펄스. =0: 원상복귀.
                        // <0: 축소(예: -1 → pulseScale 1+(-1)=0 → 글자가 점으로 응축돼 사라짐).
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoPulse(action.TargetValue, action.Duration)
                                ?.SetDelay(action.StartDelay);
                        }

                        break;
                    }

                    case DialogueActionType.Bleed:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoBleed(1f, action.Duration);
                        }

                        break;
                    }

                    case DialogueActionType.Converge:
                    {
                        foreach (var tmp in tmps)
                        {
                            if (tmp == null) 
                                continue;

                            var rect = tmp.rectTransform.rect;
                            var target = new Vector2(rect.center.x, rect.yMin + rect.height * 0.3f);
                            tmp.DoConverge(target, 1f, action.Duration);
                        }

                        break;
                    }

                    case DialogueActionType.InkMonsterAppear:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoInkMonsterAppear(action.Duration);
                        }

                        break;
                    }

                    case DialogueActionType.SuckIntoMonster:
                    {
                        var monsterTMP = _param.MonsterTMP;
                        if (monsterTMP == null)
                            break;

                        foreach (var tmp in tmps)
                        {
                            if (tmp == null || tmp == monsterTMP)
                                continue;

                            var tween = tmp.DoSuckInto(monsterTMP, action.Duration);
                            if (tween == null)
                                continue;

                            tween.SetDelay(action.StartDelay);
                            blockingDuration = Mathf.Max(
                                blockingDuration,
                                action.StartDelay + tween.Duration(false));
                        }

                        if (blockingDuration > 0f)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(blockingDuration));
                            await PlayMonsterAbsorbGrowthAsync(monsterTMP);
                            await UniTask.Delay(TimeSpan.FromSeconds(action.EndDelay));
                            delayHandled = true;
                        }

                        break;
                    }
                }
                
                if (!delayHandled)
                    await UniTask.Delay(TimeSpan.FromSeconds(blockingDuration + action.EndDelay));
            }
        }

        private async UniTask PlayMonsterAbsorbGrowthAsync(TextMeshProUGUI monsterTMP)
        {
            if (monsterTMP == null)
                return;

            var monsterTransform = monsterTMP.transform;
            monsterTransform.DOKill();

            Vector3 startScale = monsterTransform.localScale;
            Vector3 targetScale = startScale * (1f + MonsterAbsorbGrowStrength);

            PlayTween(
                monsterTransform
                    .DOScale(targetScale, MonsterAbsorbGrowDuration)
                    .SetEase(Ease.OutBack));
            PlayTween(monsterTMP.DoShake(MonsterAbsorbShakeStrength, MonsterAbsorbGrowDuration));

            await UniTask.Delay(TimeSpan.FromSeconds(MonsterAbsorbGrowDuration));

            PlayTween(monsterTMP.DoShake(0f, MonsterAbsorbShakeSettleDuration));

            await UniTask.Delay(TimeSpan.FromSeconds(MonsterAbsorbShakeSettleDuration));
        }

        private void PlayTween(Tween tween)
        {
            tween?.SetAutoKill(true);
        }
    }
}
