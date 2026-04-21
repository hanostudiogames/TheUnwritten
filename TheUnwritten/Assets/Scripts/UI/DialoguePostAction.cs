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
        public float Duration = 0; 
        // public bool IsAwait = false;
        
        public float StartDelay = 0;
        public float EndDelay = 0;
    }
    
    public class DialoguePostAction
    {
        public class Param
        {
            // public TMP_Text TMP { get; private set; } = null;
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
        
        public async UniTask ExecuteAsync()
        {
            var dialogueActions = _param?.DialogueActions;
            if (dialogueActions == null)
                return;
            
            var tmps = _param?.TMPs;
            if (tmps == null)
                return;

            for (int i = 0; i < dialogueActions.Count; ++i)
            {
                var action = dialogueActions[i];
                if(action == null)
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
                }
                
                // if (action.IsAwait)
                //     await UniTask.Delay(TimeSpan.FromSeconds(action.Duration));
                
                await UniTask.Delay(TimeSpan.FromSeconds(action.EndDelay));
            }
            
            
            // var tmps = _param?.TMPs;
            // if (tmps == null)
            //     return;
            //
            // switch (_param.ActionType)
            // {
            //     case DialoguePostActionType.DoShearAllTMP:
            //     {
            //         // await tmp.DORandomShake(10f, 1f, 0.1f).ToUniTask();
            //         for (int i = 0; i < tmps.Count; ++i)
            //         {
            //             var tmp = tmps[i];
            //             if (tmp == null)
            //                 continue;
            //             
            //             tmp.DORandomShake(7f, 2f, 0.05f);
            //         }
            //
            //         await UniTask.Delay(TimeSpan.FromSeconds(3f));
            //        
            //         for (int i = 0; i < tmps.Count; ++i)
            //         {
            //             var tmp = tmps[i];
            //             if (tmp == null)
            //                 continue;
            //
            //             tmp.DoShear(1f, 2f);
            //         }
            //         
            //         // for (int i = 0; i < tmps.Count; i++)
            //         // {
            //         //     var tmp = tmps[i];
            //         //     if (tmp == null)
            //         //         continue;
            //         //
            //         //     tmp.DoFold(0.3f, 2f);
            //         // }
            //
            //         await UniTask.Delay(TimeSpan.FromSeconds(3f));
            //         
            //         break;
            //     }
            //
            //     case DialoguePostActionType.DoFoldAllTMP:
            //     {
            //         for (int i = 0; i < tmps.Count; ++i)
            //         {
            //             var tmp = tmps[i];
            //             if (tmp == null)
            //                 continue;
            //
            //             tmp.DoFold(0.5f, 5f);
            //         }
            //         
            //         // await UniTask.Delay(TimeSpan.FromSeconds(3f));
            //         
            //         break;
            //     }
            // }
        }
    }
}

