using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AplicarLogoLEGyo
{
    [MenuItem("LEGyo/Aplicar logo biomecanico")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play para guardar el logo."); return; }
        const string ruta = "Assets/Resources/LEGyoLogoBiomecanico.png";
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) throw new System.InvalidOperationException("No se encuentra el logo.");
        importador.textureType = TextureImporterType.Default;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.maxTextureSize = 4096;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.filterMode = FilterMode.Bilinear;
        importador.SaveAndReimport();
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (escena.name != "MenuPrincipal") throw new System.InvalidOperationException("Abre MenuPrincipal.");
        int cantidad = 0;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (raiz.name != "CanvasConexion" && raiz.name != "CanvasBienvenida") continue;
            foreach (var texto in raiz.GetComponentsInChildren<Text>(true))
            {
                if (texto.name != "Marca") continue;
                texto.enabled = false;
                var existente = texto.transform.Find("LogoBiomecanico");
                var logo = existente != null ? existente.GetComponent<RawImage>() :
                    new GameObject("LogoBiomecanico", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                logo.transform.SetParent(texto.transform, false);
                logo.rectTransform.anchorMin = logo.rectTransform.anchorMax = new Vector2(.5f,.5f);
                logo.rectTransform.pivot = new Vector2(.5f,.5f);
                logo.rectTransform.anchoredPosition = Vector2.zero;
                logo.rectTransform.sizeDelta = new Vector2(600,200);
                logo.rectTransform.localScale = Vector3.one;
                logo.texture = textura;
                logo.color = Color.white;
                logo.raycastTarget = false;
                EditorUtility.SetDirty(texto);
                cantidad++;
            }
        }
        if (cantidad != 2) throw new System.InvalidOperationException("Se esperaban los dos encabezados. Encontrados: " + cantidad);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("Logo biomecanico guardado en las dos pantallas de LEGyo.");
    }
}
