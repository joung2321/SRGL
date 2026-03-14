namespace SRGL;

using System.Collections.Generic;
using SRGL.Common;

public class EffectManager
{
    private readonly JudgementLine _judgementLine;
    private Dictionary<int, ObjectPool<EffectObject>> _effectPools;

    public EffectManager(JudgementLine judgementLine)
    {
        _judgementLine = judgementLine;

        _effectPools = new Dictionary<int, ObjectPool<EffectObject>>();
    }

    /// <summary>
    /// Add a scene whose root node is a derived class of EffectObject.
    /// </summary>
    /// <param name="context">Judgement.Context</param>
    public void AddEffectType(int context, string scenePath, int poolSize = 0)
    {
        _effectPools[context] = new ObjectPool<EffectObject>(_judgementLine, scenePath, poolSize);
    }

    public void PlayEffect(Judgement judgement)
    {
        if(_effectPools.TryGetValue(judgement.Context, out ObjectPool<EffectObject> p))
        {
            EffectObject eo = p.Spawn();
            
            eo.Play(judgement, _judgementLine.GetJudgementPoint(judgement.Lane));
            eo.SetActive(true);
        }
    }
    
    public void Listen(JudgementQueue jq)
    {
        jq.NoteJudged += PlayEffect;
    }
}
