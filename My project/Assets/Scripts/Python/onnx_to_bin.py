import onnx
import numpy as np
import os
from onnx import numpy_helper

def convert_onnx_to_bin(onnx_path, output_dir):
    """
    Extrai todos os pesos (tensores) de um modelo ONNX e os salva
    como arquivos binários brutos (float32) para carregamento no Unity.
    """
    model = onnx.load(onnx_path)
    weights = model.graph.initializer
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        
    print(f"--- Neuromotive Weight Extractor ---")
    print(f"Modelo: {onnx_path}")
    
    for weight in weights:
        name = weight.name.replace("/", "_").replace(".", "_")
        numpy_data = numpy_helper.to_array(weight)
        
        # Garante que os dados sejam float32 (formato nativo do motor C#)
        if numpy_data.dtype != np.float32:
            numpy_data = numpy_data.astype(np.float32)
            
        bin_path = os.path.join(output_dir, f"{name}.bin")
        numpy_data.tofile(bin_path)
        
        print(f"Exportado: {name} | Formato: {numpy_data.shape} | Salvo em: {bin_path}")

if __name__ == "__main__":
    # Exemplo de uso para o modelo NeuromotiveAI
    convert_onnx_to_bin("results/Neuromotive_Training_v1/NeuromotiveAI.onnx", "Assets/StreamingAssets/NeuralWeights")
