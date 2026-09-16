/*
 Owner 权威的 NetworkTransform。

 NGO 1.12 的包内没有现成的 ClientNetworkTransform（Components/ 下只有 NetworkTransform、
 AnticipatedNetworkTransform、NetworkRigidbody/2D、NetworkAnimator；Samples~ 里只剩 Bootstrap），
 官方文档把 owner 权威指向 sample。真正决定权威方的是 NetworkTransform.cs:3156 的
 protected virtual bool OnIsServerAuthoritative()（默认 true），
 且 NetworkTransform.cs:2864 已按它算出：CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner。
 所以这里只要重写该方法返回 false，owner 客户端就能写自己的 transform。
 */
using Unity.Netcode.Components;

public class OwnerAuthorityTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
