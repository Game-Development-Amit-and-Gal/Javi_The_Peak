using UnityEngine;

public class CameraPageToggle : MonoBehaviour
{
    public Book book;
    public GameObject cameraPage;

    void Awake()
    {
        if (!book)
            book = Object.FindFirstObjectByType<Book>();
    }

    void Update()
    {
        if (!book || !cameraPage)
            return;

        
        bool shouldShow = (book.currentPage == 0 && !book.IsPageDragging);

        if (cameraPage.activeSelf != shouldShow)
            cameraPage.SetActive(shouldShow);
    }
}
