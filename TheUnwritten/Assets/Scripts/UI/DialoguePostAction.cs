using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Common;
using TMPro;
using UI.Effects;

namespace UI
{
    public class DialoguePostAction
    {
        public class Param
        {
            // public TMP_Text TMP { get; private set; } = null;
            public List<TextMeshProUGUI> TMPs { get; private set; } = new();
            public int ActIndex { get; private set; }
            public int SceneIndex { get; private set; }
            public DialoguePostActionType ActionType { get; private set; } = DialoguePostActionType.None;
            public Action OnComplete { get; private set; } = null;

            public Param(List<TextMeshProUGUI> tmps, int actIndex, int sceneIndex)
            {
                TMPs = tmps;
                ActIndex = actIndex;
                SceneIndex = sceneIndex;
            }

            public Param WithDialoguePostActionType(DialoguePostActionType actionType)
            {
                ActionType = actionType;
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
            var tmps = _param?.TMPs;
            if (tmps == null)
                return;

            switch (_param.ActionType)
            {
                case DialoguePostActionType.DoShearAllTMP:
                {
                    // await tmp.DORandomShake(10f, 1f, 0.1f).ToUniTask();
                    for (int i = 0; i < tmps.Count; ++i)
                    {
                        var tmp = tmps[i];
                        if (tmp == null)
                            continue;
                        
                        tmp.DORandomShake(7f, 2f, 0.05f);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(3f));
                   
                    for (int i = 0; i < tmps.Count; ++i)
                    {
                        var tmp = tmps[i];
                        if (tmp == null)
                            continue;

                        tmp.DoShear(1f, 2f);
                    }
                    
                    // for (int i = 0; i < tmps.Count; i++)
                    // {
                    //     var tmp = tmps[i];
                    //     if (tmp == null)
                    //         continue;
                    //
                    //     tmp.DoFold(0.3f, 2f);
                    // }

                    await UniTask.Delay(TimeSpan.FromSeconds(3f));
                    
                    break;
                }

                case DialoguePostActionType.DoFoldAllTMP:
                {
                    for (int i = 0; i < tmps.Count; ++i)
                    {
                        var tmp = tmps[i];
                        if (tmp == null)
                            continue;

                        tmp.DoFold(0.5f, 5f);
                    }
                    
                    // await UniTask.Delay(TimeSpan.FromSeconds(3f));
                    
                    break;
                }
            }
        }
    }
}

