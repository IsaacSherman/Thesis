function [success] = saveModel(path, model)
success = false;
disp('Saving... path = ');
disp(path);
save(path, 'model')
success = true;
end
