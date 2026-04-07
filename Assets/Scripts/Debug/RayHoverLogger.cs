using System.Reflection;
using System.Text;
using Oculus.Interaction;
using UnityEngine;

[RequireComponent(typeof(RayInteractor))]
public class RayHoverLogger : MonoBehaviour
{
  private RayInteractor _rayInteractor;
  private string _lastSnapshot;

  void Awake()
  {
    _rayInteractor = GetComponent<RayInteractor>();
  }

  void Update()
  {
    string snapshot = BuildSnapshot();

    if (snapshot != _lastSnapshot)
    {
      _lastSnapshot = snapshot;
      Debug.Log(snapshot);
    }
  }

  private string BuildSnapshot()
  {
    var sb = new StringBuilder();

    sb.Append("[Ray] ");
    sb.Append("HasCandidate=").Append(_rayInteractor.HasCandidate);

    sb.Append(" | Candidate=").Append(GetUnityName(_rayInteractor.Candidate));
    sb.Append(" | Interactable=").Append(GetUnityName(_rayInteractor.Interactable));
    sb.Append(" | Selected=").Append(GetUnityName(_rayInteractor.SelectedInteractable));

    sb.Append(" | End=").Append(_rayInteractor.End);

    try
    {
      object collisionInfo = _rayInteractor.CollisionInfo;
      sb.Append(" | CollisionInfo=").Append(DescribeObject(collisionInfo));
    }
    catch
    {
      sb.Append(" | CollisionInfo=<unavailable>");
    }

    return sb.ToString();
  }

  private static string GetUnityName(object obj)
  {
    if (obj == null) return "<none>";

    if (obj is Component c) return c.gameObject.name;
    if (obj is GameObject go) return go.name;
    if (obj is Object unityObj) return unityObj.name;

    return obj.ToString();
  }

  private static string DescribeObject(object obj)
  {
    if (obj == null) return "<null>";

    var type = obj.GetType();
    var sb = new StringBuilder();
    sb.Append(type.Name).Append("{");

    bool wroteAny = false;

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;

      object value;
      try
      {
        value = prop.GetValue(obj, null);
      }
      catch
      {
        continue;
      }

      if (wroteAny) sb.Append(", ");
      sb.Append(prop.Name).Append("=");

      if (value is Component c)
        sb.Append(c.gameObject.name);
      else if (value is GameObject go)
        sb.Append(go.name);
      else
        sb.Append(value != null ? value.ToString() : "<null>");

      wroteAny = true;
    }

    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
    {
      object value;
      try
      {
        value = field.GetValue(obj);
      }
      catch
      {
        continue;
      }

      if (wroteAny) sb.Append(", ");
      sb.Append(field.Name).Append("=");

      if (value is Component c)
        sb.Append(c.gameObject.name);
      else if (value is GameObject go)
        sb.Append(go.name);
      else
        sb.Append(value != null ? value.ToString() : "<null>");

      wroteAny = true;
    }

    sb.Append("}");
    return sb.ToString();
  }
}
