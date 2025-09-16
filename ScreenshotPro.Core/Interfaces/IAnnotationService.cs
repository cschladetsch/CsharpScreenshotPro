using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Interfaces;

public interface IAnnotationService
{
    void AddAnnotation(Screenshot screenshot, Annotation annotation);
    void RemoveAnnotation(Screenshot screenshot, string annotationId);
    void UpdateAnnotation(Screenshot screenshot, string annotationId, Annotation updatedAnnotation);
    List<Annotation> GetAnnotations(Screenshot screenshot);
    void ClearAnnotations(Screenshot screenshot);

    Task<byte[]> RenderAsync(Screenshot screenshot);
    Task<byte[]> RenderLayerAsync(Screenshot screenshot, int layerIndex);

    void Undo(Screenshot screenshot);
    void Redo(Screenshot screenshot);
    bool CanUndo(Screenshot screenshot);
    bool CanRedo(Screenshot screenshot);

    event EventHandler<AnnotationEventArgs> AnnotationAdded;
    event EventHandler<AnnotationEventArgs> AnnotationUpdated;
    event EventHandler<AnnotationEventArgs> AnnotationRemoved;
}