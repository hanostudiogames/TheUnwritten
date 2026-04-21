using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Common;
using TMPro;
using UI.Effects;

namespace UI
{
    [Serializable]
    public class DialogueAction
    {
        public DialogueActionType DialogueActionType = DialogueActionType.None;
        public int TmpCount = 0;
        public float Duration = 0; 
        // public bool IsAwait = false;
        
        public float StartDelay = 0;
        public float EndDelay = 0;
    }
    
    public class DialoguePostAction
    {
        public class Param
        {
            public List<TextMeshProUGUI> TMPs { get; private set; } = new();
            
            public int ActIndex { get; private set; }
            public int SceneIndex { get; private set; }
            
            public List<DialogueAction> DialogueActions { get; private set; } = new();

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
                
                await UniTask.Delay(TimeSpan.FromSeconds(action.StartDelay));
                
                switch (action.DialogueActionType)
                {
                    case DialogueActionType.Shear:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoShear(1f, action.Duration);
                        }

                        break;
                    }

                    case DialogueActionType.Fold:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoFold(0.5f, action.Duration);
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
                            tmp?.DoMelt(1f, action.Duration);
                        }

                        break;
                    }

                    case DialogueActionType.RandomMelt:
                    {
                        foreach (var tmp in tmps)
                        {
                            tmp?.DORandomMelt(1f, action.Duration, 0.05f);
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
                        foreach (var tmp in tmps)
                        {
                            tmp?.DoPulse(0.25f, action.Duration);
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
                            if (tmp == null) continue;

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
                }
                
                // if (action.IsAwait)
                //     await UniTask.Delay(TimeSpan.FromSeconds(action.Duration));
                
                await UniTask.Delay(TimeSpan.FromSeconds(action.EndDelay));
            }
        }
    }
}

