export function pickFiles(accept: string, onpick: (files: File[]) => void) {
  const input = document.createElement('input');
  input.type = 'file';
  input.multiple = true;
  input.accept = accept;
  input.onchange = () => {
    if (input.files?.length) onpick(Array.from(input.files));
  };
  input.click();
}