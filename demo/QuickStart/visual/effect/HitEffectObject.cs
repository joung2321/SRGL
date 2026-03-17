using Godot;

using SRGL;
using SRGL.Common;

public partial class HitEffectObject : EffectObject
{
    [Export] private AnimatedSprite2D _spriteAnimation;

    protected override void OnPlay(Judgement judgement)
    {
        _spriteAnimation.Stop();

        switch(judgement.PartitionIndex)
        {
            case 0:
            case 1:
            _spriteAnimation.Play("pure");
            break;

            case 2:
            _spriteAnimation.Play("far");
            break;

            default:
            _spriteAnimation.Play("default"); // empty animation
            break;
        }
    }
}